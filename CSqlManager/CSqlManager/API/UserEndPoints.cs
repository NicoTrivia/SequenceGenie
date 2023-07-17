﻿namespace CSqlManager;

public class UserEndPoints: SecureEnpoint
{
    public static void MapEndPoints(WebApplication app)
    {
        app.MapGet("/user", GetAll);
        app.MapGet("/user/tenant/{Tenant}", GetByTenant);
        app.MapGet("/user/{Id}", GetById);
        app.MapPost("/authenticate", UserLogin);
        
        app.MapPost("/user", Create);
        
        app.MapPut("/user", Update);
        app.MapPut("/password", Update);
        app.MapDelete("/user/{Id}", DeleteById);
    }
    
    public static IResult GetAll(HttpContext context)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid || (claims.Profile != "ADMIN" && claims.Profile != "OPERATOR")) {
            MyLogManager.Error("ERROR 401 : Invalid JWT/PROFILE : "+ claims);
            return Results.Unauthorized();
        }
        var access = new UserAccess();
        var list = access.GetUsers();

        return Results.Ok(list);
    }

    public static IResult GetByTenant(HttpContext context, string Tenant)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid || claims.Tenant != Tenant && claims.Profile != "ADMIN" && claims.Profile != "OPERATOR") {
            MyLogManager.Error("ERROR 401 : Invalid JWT : "+ claims);
            return Results.Unauthorized();
        }
        var access = new UserAccess();
        var list = access.GetUsersByTenant(Tenant);

        return Results.Ok(list);
    }
    
    public static IResult GetById(HttpContext context, int Id)
    {
        var access = new UserAccess();
        var user = access.GetUserById(Id);
        JwtClaims claims = getJwtClaims(context);
        if ((user != null) && (!claims.Valid || claims.Tenant != user.tenant && claims.Profile != "ADMIN" && claims.Profile != "OPERATOR")) {
            MyLogManager.Error("ERROR 401 : Invalid JWT : "+ claims);
            return Results.Unauthorized();
        }
     
        return Results.Ok(user);
    }

    public static IResult Create(HttpContext context, User user)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid || (claims.Profile != "ADMIN" && claims.Profile != "OPERATOR")) {
            MyLogManager.Error("ERROR 401 : Invalid JWT/PROFILE : "+ claims);
            return Results.Unauthorized();
        }
        MyLogManager.Log($"USER POST {user.id} - {user.login} - {user.firstname}- {user.lastname}");
        
        var access = new UserAccess();
        access.Create(user);
       MyLogManager.Log($"User created : {user.login} - {user.tenant} by {claims.User} / {claims.Tenant}");
        return Results.Ok(user);
    }
    public static IResult Update(HttpContext context, User user)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid || (claims.Profile != "ADMIN" && claims.Profile != "OPERATOR" && (user.login != claims.User || user.tenant != claims.Tenant))) {
            MyLogManager.Error("ERROR 401 : Invalid JWT/PROFILE : "+ claims);
            return Results.Unauthorized();
        }
        MyLogManager.Log($"USER PUT {user}");
       
        var access = new UserAccess();
        access.Update(user);
       MyLogManager.Log($"User updated : {user.login} - {user.tenant} by {claims.User} / {claims.Tenant}");

        return Results.Ok(user);
    }

    
    public static IResult UpdatePassword(HttpContext context, User user)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid || (claims.Profile != "ADMIN" && claims.Profile != "OPERATOR" && (user.login != claims.User || user.tenant != claims.Tenant))) {
            MyLogManager.Error("ERROR 401 : Invalid JWT/PROFILE : "+ claims);
            return Results.Unauthorized();
        }
        MyLogManager.Log($"USER PASSWORD: {user.id}");
       
        var access = new UserAccess();
        access.UpdatePassword(user);
       MyLogManager.Log($"User password updated : {user.login} - {user.tenant} by {claims.User} / {claims.Tenant}");

        return Results.Ok(user);
    }


    public static IResult UserLogin(HttpContext context)
    {
        var Tenant = "";
        var Login = "";
        var Password = "";
        foreach (var formPart in context.Request.Form) {
            if (formPart.Key == "tenant") {
               Tenant = formPart.Value;
            }
            else if (formPart.Key == "login") {
               Login = formPart.Value;
            }
            else if (formPart.Key == "password") {
               Password = formPart.Value;
            }
        }

        MyLogManager.Log($"Trying to login in tenant : : {Tenant} as : {Login} with password : ******** ");
        
        UserAccess access = new UserAccess();
        TenantAccess tenantAccess = new TenantAccess();
        User? user = access.Login(Tenant,Login,Password);

        //string level = tenantAccess.GetTenantByCode(user.tenant).level;
        if (user != null)
        {
            string profile = user.profile.ToString();
            user.jwt = User.GenerateJwtToken(user.login, user.id, user.tenant, profile);
        }
        
        return Results.Ok(user);
    }
        
    public static IResult DeleteById(HttpContext context, int Id)
    {
        JwtClaims claims = getJwtClaims(context);
        if (!claims.Valid || (claims.Profile != "ADMIN" && claims.Profile != "OPERATOR")) {
            MyLogManager.Error("ERROR 401 : Invalid JWT/PROFILE : "+ claims);
            return Results.Unauthorized();
        }
        var access = new UserAccess();
        var success = access.DeleteUserById(Id);
       MyLogManager.Log($"User deleted : {Id} by {claims.User} / {claims.Tenant}");

        return Results.Ok(success);
    }

}