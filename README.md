How to Debug?



Angular: ng serve



Backend:

Start the app

Execute the command to create the DB:
docker compose up -d

Docker compose down -v

o -v também deleta os volumes



.NET Migrations:



dotnet ef migrations add InitialCreate -s API -p Infrastructure
dotnet ef migrations remove -s API -p Infrastructure
dotnet ef database update -s API -p Infrastructure

