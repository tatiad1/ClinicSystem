using ClinicSystem.Web.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicSystem.Web.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var errors = new Dictionary<string, List<string>>();

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null)
                    continue;

                // ვეძებთ ამ ტიპის DTO-სთვის დარეგისტრირებულ validator-ს
                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                    continue;

                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument),
                    context.HttpContext.RequestAborted);

                foreach (var failure in result.Errors)
                {
                    if (!errors.TryGetValue(failure.PropertyName, out var messages))
                    {
                        messages = new List<string>();
                        errors[failure.PropertyName] = messages;
                    }

                    messages.Add(failure.ErrorMessage);
                }
            }

            if (errors.Count > 0)
            {
                var response = ApiResponse<object>.Fail(
                    "Validation error(s)",
                    errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));

                context.Result = new BadRequestObjectResult(response);
                return;
            }

            await next();
        }
    }
}