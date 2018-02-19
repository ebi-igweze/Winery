dotnet restore src/Winery
dotnet build src/Winery

dotnet restore tests/Winery.Tests
dotnet build tests/Winery.Tests
dotnet test tests/Winery.Tests
