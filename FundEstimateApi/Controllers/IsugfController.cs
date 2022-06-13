using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;
using Services;
using System;
using System.Collections.Generic;
using System.IO;
using ViewModel;

namespace FundEstimateApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IsugfController : ControllerBase
    {
        private readonly ILogger<IsugfController> _logger;
        private IDatabaseService _databaseService { get; }
        public IsugfController(ILogger<IsugfController> logger, IDatabaseService databaseService)
        {
            _logger = logger;
            _databaseService = databaseService;
        }

        [HttpGet]
        [Route("GetFundEstimate")]
        public IActionResult GetFundEstimate()
        {
            StateViewModel<List<FundEstimateModel>> state = new StateViewModel<List<FundEstimateModel>>();
            try
            {
                state = _databaseService.GetFundEstimate();

                if (state.Code == 200 && state.Response.Count > 0)
                    _databaseService.SetSentStatus(state);
                return Ok(state);
            }
            catch (Exception ex)
            {
                var log = new LoggerConfiguration().WriteTo.File($@"{Directory.GetCurrentDirectory()}\Log.txt").CreateLogger();

                log.Error(ex.Message);
                state.Code = 500;
                state.Msg = "Error";
                return BadRequest(state);
            }
        }
    }
}
