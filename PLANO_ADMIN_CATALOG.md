# Plano: Admin — Gestão de Catálogo (Produtos, Marcas, Categorias, Imagens)

## 1. Objetivo

Transformar a aba "Stock" do admin em uma aba **"Catalog"** completa com:
- CRUD de produtos (criar, editar, excluir)
- Upload de imagens para `wwwroot/images/products/`
- Dropdowns de marca e categoria com dados do banco
- Gestão de marcas e categorias (adicionar/remover) via popup
- Proteção: não permite excluir marca/categoria com produtos associados
- Manter edição inline de estoque

---

## 2. Backend — Novos Endpoints

### 2.1. Brand & Type Management (em `AdminController`)

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/admin/brands` | Lista marcas distintas (string[]) |
| `POST` | `/api/admin/brands` | Adiciona marca `{ name: string }` |
| `DELETE` | `/api/admin/brands/{name}` | Remove marca (400 se houver produtos) |
| `GET` | `/api/admin/types` | Lista categorias distintas (string[]) |
| `POST` | `/api/admin/types` | Adiciona categoria `{ name: string }` |
| `DELETE` | `/api/admin/types/{name}` | Remove categoria (400 se houver produtos) |

### 2.2. Product CRUD (em `AdminController`)

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/admin/products` | Cria produto (body: `CreateProductDto`) |
| `PUT` | `/api/admin/products/{id}` | Atualiza produto (body: `UpdateProductDto`) |
| `DELETE` | `/api/admin/products/{id}` | Exclui produto |

### 2.3. Image Upload

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/admin/images/upload` | Recebe `IFormFile`, salva em `wwwroot/images/products/{guid}{ext}`, retorna `{ url }` |

---

## 3. Backend — Novos/Modificados Arquivos

### 3.1. `API/DTOs/CreateProductDto.cs` (já existe, usar como está)

```csharp
public class CreateProductDto {
    [Required] public string Name { get; set; }
    [Required] public string Description { get; set; }
    [Range(0.01, double.MaxValue)] [Required] public decimal Price { get; set; }
    [Required] public string PictureUrl { get; set; }
    [Required] public string Type { get; set; }
    [Required] public string Brand { get; set; }
    [Range(1, int.MaxValue)] [Required] public int QuantityInStock { get; set; }
}
```

### 3.2. `API/DTOs/UpdateProductDto.cs` (criar)

```csharp
public class UpdateProductDto {
    [Required] public string Name { get; set; }
    [Required] public string Description { get; set; }
    [Range(0.01, double.MaxValue)] [Required] public decimal Price { get; set; }
    [Required] public string PictureUrl { get; set; }
    [Required] public string Type { get; set; }
    [Required] public string Brand { get; set; }
    [Range(0, int.MaxValue)] [Required] public int QuantityInStock { get; set; }
}
```

### 3.3. `API/Controllers/AdminController.cs` (modificar)

Adicionar ao `AdminController`:

```csharp
// Brand management
[HttpGet("brands")]
public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    => Ok(await unit.Repository<Product>().ListAsync(new BrandListSpecification()));

[HttpPost("brands")]
public async Task<ActionResult> AddBrand(AddBrandDto dto) { ... }

[HttpDelete("brands/{name}")]
public async Task<ActionResult> DeleteBrand(string name) {
    var count = await unit.Repository<Product>().CountAsync(
        new ProductSpecification(new ProductSpecParams { Brands = [name] }));
    if (count > 0) return BadRequest($"Cannot delete: {count} product(s) use this brand");
    return NoContent();
}

// Type management (mesma lógica)
[HttpGet("types")]
[HttpPost("types")]
[HttpDelete("types/{name}")]
// ...

// Product CRUD
[HttpPost("products")]
[InvalidateCache("api/products|")]
public async Task<ActionResult<Product>> CreateProduct(CreateProductDto dto) { ... }

[HttpPut("products/{id}")]
[InvalidateCache("api/products|")]
public async Task<ActionResult> UpdateProduct(int id, UpdateProductDto dto) { ... }

[HttpDelete("products/{id}")]
[InvalidateCache("api/products|")]
public async Task<ActionResult> DeleteProduct(int id) { ... }

// Image upload
[HttpPost("images/upload")]
public async Task<ActionResult<ImageUploadResult>> UploadImage(IFormFile file) {
    // Validar extensão (.png, .jpg, .jpeg, .webp)
    // Gerar nome: Guid + extensão original
    // Salvar em wwwroot/images/products/
    // Retornar { url: "/images/products/guid.ext" }
}
```

### 3.4. `API/DTOs/AddBrandDto.cs` e `AddTypeDto.cs` (criar)

```csharp
public class AddBrandDto { [Required] public string Name { get; set; } }
public class AddTypeDto { [Required] public string Name { get; set; } }
public class ImageUploadResult { public string Url { get; set; } }
```

---

## 4. Frontend — AdminService

Modificar `client/src/app/core/services/admin.service.ts`:

```typescript
// Product CRUD
createProduct(dto: CreateProductDto): Observable<Product>
updateProduct(id: number, dto: UpdateProductDto): Observable<void>
deleteProduct(id: number): Observable<void>

// Brands
getBrands(): Observable<string[]>
addBrand(name: string): Observable<void>
deleteBrand(name: string): Observable<void>

// Types
getTypes(): Observable<string[]>
addType(name: string): Observable<void>
deleteType(name: string): Observable<void>

// Image
uploadImage(file: File): Observable<{ url: string }>
```

### Novos models:

```typescript
// client/src/app/shared/models/createProduct.ts
export type CreateProduct = {
  name: string;
  description: string;
  price: number;
  pictureUrl: string;
  type: string;
  brand: string;
  quantityInStock: number;
}
```

---

## 5. Frontend — Product Dialog

### `client/src/app/features/admin/product-dialog/product-dialog.component.ts`

MatDialog com `@Inject(MAT_DIALOG_DATA) data: { product?: Product }`:
- Se `data.product` existe → modo edição, pré-preenche campos
- Se não → modo criação

### `product-dialog.component.html`

Formulário com:

```
┌──────────────────────────────────┐
│  [Add/Edit] Product              │
├──────────────────────────────────┤
│  Image: [Choose File] [preview]  │
│  Name:    [________________]     │
│  Description: [________________] │
│  Price:   [______]               │
│  Brand:   [dropdown ▼] [Manage]  │
│  Type:    [dropdown ▼] [Manage]  │
│  Stock:   [______]               │
├──────────────────────────────────┤
│         [Cancel]    [Save]       │
└──────────────────────────────────┘
```

### Fluxo de upload:
1. User clica "Choose File", seleciona imagem
2. Preview é exibido
3. Ao salvar, se houver nova imagem → faz upload primeiro → recebe URL → envia produto com URL
4. Se não alterou imagem (modo edição) → envia URL existente

---

## 6. Frontend — Brand/Type Dialog

### `client/src/app/features/admin/brand-type-dialog/brand-type-dialog.component.ts`

Duas abas usando `<mat-tab-group>`:

### `brand-type-dialog.component.html`

```
┌──────────────────────────────────┐
│  Manage Brands & Types           │
├──────────────────────────────────┤
│  [Brands] [Types]                │
│                                  │
│  ─── Brands ───                  │
│  [input + Add button]            │
│  • Nike              [✕]         │
│  • Adidas            [✕]         │
│  • Puma              [✕]         │
│  (se tentar deletar com produtos │
│   → snackbar: "Cannot delete:    │
│     3 product(s) use this brand")│
└──────────────────────────────────┘
```

---

## 7. Frontend — Admin Component

### Modificar `admin.component.ts`

- Renomear `stockSearchQuery` → `catalogSearchQuery`
- Manter `products`, `filteredProducts`, `editingStockId`, `editingStockValue`
- Adicionar método `openProductDialog(product?: Product)`
- Adicionar método `openBrandTypeDialog()`
- Adicionar método `deleteProduct(product: Product)` com confirmação

### Modificar `admin.component.html`

Substituir:

```html
<mat-tab label="Stock">
  ...
</mat-tab>
```

Por:

```html
<mat-tab label="Catalog">
  <div class="mt-6">
    <!-- Header com stats + Add Product + Manage Brands/Types -->
    <div class="flex flex-wrap justify-between items-center gap-4 mb-6">
      <div class="flex gap-4">
        <!-- 3 cards de stats (Total, Out, Low) iguais aos atuais -->
      </div>
      <div class="flex gap-2">
        <button mat-stroked-button (click)="openBrandTypeDialog()">
          <mat-icon>manage_search</mat-icon> Brands & Types
        </button>
        <button mat-flat-button color="primary" (click)="openProductDialog()">
          <mat-icon>add</mat-icon> Add Product
        </button>
      </div>
    </div>

    <!-- Search (igual ao atual) -->
    ...

    <!-- Tabela com colunas: Image, Name, Price, Brand, Type, Status, Stock, Actions -->
    <table mat-table [dataSource]="filteredProducts">
      <!-- Coluna Image: thumbnail -->
      <!-- Coluna Name: nome + brand/type -->
      <!-- Coluna Price: preço -->
      <!-- Coluna Status: in/out/low stock (igual) -->
      <!-- Coluna Stock: inline edit (igual) -->
      <!-- Coluna Actions: Edit + Delete buttons -->
    </table>
  </div>
</mat-tab>
```

### Coluna de Actions:
```html
<ng-container matColumnDef="actions">
  <th mat-header-cell *matHeaderCellDef>Actions</th>
  <td mat-cell *matCellDef="let product">
    <button mat-icon-button (click)="openProductDialog(product)" matTooltip="Edit">
      <mat-icon>edit</mat-icon>
    </button>
    <button mat-icon-button (click)="deleteProduct(product)" matTooltip="Delete">
      <mat-icon>delete</mat-icon>
    </button>
  </td>
</ng-container>
```

---

## 8. Fluxo completo (exemplo: criar produto)

1. Admin clica **"Add Product"**
2. Abre `ProductDialog`
3. Admin preenche: Name, Description, Price, seleciona imagem, escolhe Brand do dropdown (ou clica "Manage" para adicionar uma), escolhe Type, preenche Stock
4. Clica **Save**
   - Se tem nova imagem: `POST /api/admin/images/upload` → recebe URL
   - `POST /api/admin/products` com todos os dados (incluindo `pictureUrl`)
5. Dialog fecha, tabela atualiza com novo produto
6. Snackbar: "Product created successfully"

---

## 9. Proteções

- **Deletar Brand**: verifica `COUNT(*) FROM Products WHERE Brand = @name` — se > 0, retorna 400 com mensagem
- **Deletar Type**: mesma lógica
- **Deletar Product**: confirma via `DialogService.confirm()` antes de chamar o endpoint
- **Upload**: validar extensão (`.png`, `.jpg`, `.jpeg`, `.webg`) e tamanho máximo

---

## 10. Arquivos envolvidos (resumo)

### Criar:
| Caminho | Descrição |
|---------|-----------|
| `API/DTOs/UpdateProductDto.cs` | DTO de atualização |
| `API/DTOs/AddBrandDto.cs` | DTO add brand |
| `API/DTOs/AddTypeDto.cs` | DTO add type |
| `API/DTOs/ImageUploadResult.cs` | Resultado do upload |
| `client/src/app/shared/models/createProduct.ts` | Modelo p/ criar produto |
| `client/src/app/features/admin/product-dialog/` | Componente do dialog de produto |
| `client/src/app/features/admin/brand-type-dialog/` | Componente do dialog de brand/type |

### Modificar:
| Caminho | Descrição |
|---------|-----------|
| `API/Controllers/AdminController.cs` | + brand/type/product/image endpoints |
| `client/src/app/core/services/admin.service.ts` | + métodos CRUD + brand/type/image |
| `client/src/app/features/admin/admin.component.ts` | + lógica do catalog tab |
| `client/src/app/features/admin/admin.component.html` | + catalog tab template |
