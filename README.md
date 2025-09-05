# DotNetProjects
Repository for projects using .NET technology.
- ProductApi is a webapi built on .NET 9. The API provide a regular functions for eCommerce products. This API includes JWT authentication and authorization on the product api endponts. In order to test it, login first through /api/auth/login to get the JWT token. Then revoke product api endpoints with authorization bearer token. To make it simple, this API connects to Entityframework in-memory db filled with predefined product data. The API docment is built up with Swagger. View the API document through [/swagger/index.html].
