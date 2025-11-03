using BaseballApp.Models;
using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

public class BaseballController : Controller
{
    private readonly IBaseballDbService _baseballDbService;
    private readonly ILogger<BaseballController> _logger;

    public BaseballController(IBaseballDbService baseballDbService, ILogger<BaseballController> logger)
    {
        _baseballDbService = baseballDbService;
        _logger = logger;
    }


}