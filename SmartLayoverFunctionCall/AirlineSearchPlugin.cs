using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SmartLayoverFunctionCall;

// ReSharper disable once ClassNeverInstantiated.Global
public class AirlineSearchPlugin
{
    [KernelFunction("SearchFlight")]
    [Description("Returns a list of airlines that fly between cities")]
    public string[] SearchFlight(string fromCity, string toCity)
    {
        return
        [
            "British Airways",
            "Virgin Atlantic",
            "American Airlines",
            "Delta Airlines",
            "United Airlines",
            "SK FunctionAirways",
            $"{fromCity} {toCity} Airlines"
        ];
    }
}