using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MotoRent.API.Filters
{
    public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order { get; set; } = int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is ArgumentException argumentException)
            {
                context.Result = new ObjectResult(new { message = argumentException.Message })
                {
                    StatusCode = 400
                };
                context.ExceptionHandled = true;
            }
            else if (context.Exception is Exception)
            {
                context.Result = new ObjectResult(new { message = "An error occurred" })
                {
                    StatusCode = 500
                };
                context.ExceptionHandled = true;
            }
        }
    }
}