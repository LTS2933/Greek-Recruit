using Microsoft.AspNetCore.Mvc;

public class ErrorController : Controller
{
    //Returns different error pages
    [Route("Error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        if (statusCode == 404)
        {
            return View("NotFound");
        }
        else
        {
            return View("AccessDenied");
        }
    }
}

//No need for authorization annotation