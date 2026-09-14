using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tavstal.YggdrasilSharp.Models.Attributes;

/// <summary>
/// An attribute that requires a specific custom header to be present in the request.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireCustomHeaderAttribute : ActionFilterAttribute
{
    private readonly string _headerName;
    private readonly string _expectedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireCustomHeaderAttribute"/> class.
    /// </summary>
    /// <param name="headerName">The name of the required header.</param>
    /// <param name="expectedValue">The expected value of the required header.</param>
    public RequireCustomHeaderAttribute(string headerName, string expectedValue)
    {
        _headerName = headerName;
        _expectedValue = expectedValue;
    }

    /// <summary>
    /// Called before the action method is executed.
    /// </summary>
    /// <param name="context">The context for the action.</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Check if the header exists
        if (!context.HttpContext.Request.Headers.ContainsKey(_headerName) ||
            context.HttpContext.Request.Headers[_headerName] != _expectedValue)
        {
            context.Result = new UnauthorizedResult();
        }

        base.OnActionExecuting(context);
    }
}
