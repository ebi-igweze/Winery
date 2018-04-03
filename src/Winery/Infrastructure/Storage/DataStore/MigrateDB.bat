REM Migrate Database
C:\Users\Ebillionaire\.nuget\packages\fluentmigrator\1.6.2\tools\Migrate.exe --db=sqlserver --target=bin\Debug\net461\Winery.exe --c=WineryDB %*

REM Update Database DBML file
.\Infrastructure\Storage\DataStore\Utils\SqlMetal.exe /server:"(local)" /database:Winery /dbml:"./Infrastructure/Storage/DataStore/Storage.dbml"
