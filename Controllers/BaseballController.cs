using BaseballApp.Models;
using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

public class BaseballController : Controller
{
    private readonly IBaseballDbService _baseballDataService;
    private readonly ILogger<BaseballController> _logger;

    public BaseballController(IBaseballDbService baseballDataService, ILogger<BaseballController> logger)
    {
        _baseballDataService = baseballDataService;
        _logger = logger;
    }

}