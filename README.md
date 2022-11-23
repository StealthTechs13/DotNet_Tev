# Db migration commands for impact

create a new migration - 

```sh
dotnet ef migrations add "comment" --project Tev.DAL -s Tev.API
```

update db -

```sh
dotnet ef database update --project Tev.DAL -s Tev.API
```