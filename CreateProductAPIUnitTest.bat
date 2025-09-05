dotnet new xunit -n ProductApi.Tests
dotnet add ProductApi.Tests/ProductApi.Tests.csproj reference ProductApi/ProductApi.csproj
#dotnet add ProductApi.Tests package Microsoft.EntityFrameworkCore.InMemory
#dotnet add ProductApi.Tests package Moq
#dotnet add ProductApi.Tests package FluentAssertions
#dotnet add ProductApi.Tests package Microsoft.AspNetCore.Mvc.Testing
#dotnet add ProductApi.Tests package coverlet.collector
#dotnet add ProductApi.Tests package Microsoft.NET.Test.Sdk
#dotnet add ProductApi.Tests package xunit.runner.visualstudio
#dotnet add ProductApi.Tests package Microsoft.AspNetCore.TestHost   
#dotnet restore ProductApi.Tests/ProductApi.Tests.csproj
#dotnet build ProductApi.Tests/ProductApi.Tests.csproj
#dotnet test ProductApi.Tests/ProductApi.Tests.csproj --collect:"XPlat Code Coverage"    
pause
exit /b 0
