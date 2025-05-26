using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.Helpers;

namespace WebApi.Filters
{
    public class UserIdValidationFilter : IActionFilter
    {
        private readonly ILogger<UserIdValidationFilter> _logger;

        public UserIdValidationFilter(ILogger<UserIdValidationFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            string? userId = context.HttpContext.GetUserId();
            
            if (userId == null)
            {
                _logger.LogWarning("API request without valid user ID: {Path}", 
                    context.HttpContext.Request.Path);
                
                context.Result = new UnauthorizedObjectResult(new { error = "UserId is required" });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}