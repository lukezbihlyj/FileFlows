using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FileFlows.Server.Authentication;

/// <summary>
/// FileFlows authentication attribute
/// </summary>
public class FileFlowsAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// Gets or sets the role
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Constructs a new instance of the FileFlows authorize filter
    /// </summary>
    /// <param name="role">the role</param>
    public FileFlowsAuthorizeAttribute(UserRole role = (UserRole)0)
    {
        Role = role;
    }
    
    /// <summary>
    /// Handles the on on authorization
    /// </summary>
    /// <param name="context">the context</param>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (AuthenticationHelper.GetSecurityMode() == SecurityMode.Off)
            return;

        UserRole roleToTest = Role;
        
        // Check if the action method has the AllowAnonymous attribute
        if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
        {
            var allowAnonymousAttribute = actionDescriptor.MethodInfo.GetCustomAttributes(inherit: true)
                .OfType<AllowAnonymousAttribute>()
                .FirstOrDefault();

            if (allowAnonymousAttribute != null)
            {
                // The action method has the AllowAnonymous attribute applied
                // Skip the authorization check
                return;
            }
            var authorizeAttribute = actionDescriptor.MethodInfo.GetCustomAttributes(inherit: true)
                .OfType<FileFlowsAuthorizeAttribute>()
                .FirstOrDefault();
            if (authorizeAttribute != null)
                roleToTest = authorizeAttribute.Role; // method level authorization in place for this
        }
        
        var user = context.HttpContext.GetLoggedInUser().Result;
        if(user == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if ((int)roleToTest == 0)
            return; // any role
        
        if(roleToTest == UserRole.Admin)
        {
            if(user.Role != UserRole.Admin)
                context.Result = new UnauthorizedResult();
            return;
        }
        
        if ((user.Role & roleToTest) == 0) // they require any of the enums
        {
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}