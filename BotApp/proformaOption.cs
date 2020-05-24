using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BotApp
{

    public enum AccrualRates
    {
        LowreRate,
        MiddleRate,
        UpperRate
    };

    [Serializable]
    class profomaOption
    {
        [Prompt(new string[] { "What is your name?" })]
        public string Name { get; set; }

        [Prompt("How can Ankit contact you? You can enter either your email id or twitter handle (@something)")]
        public string Contact { get; set; }

        [Prompt("What's your feedback?")]
        public string Feedback { get; set; }

        

        [Prompt("Select the Rate Type?")]
        public AccrualRates AccrualRateType;

        public static IForm<profomaOption> BuildForm()
        {
            var builder = new FormBuilder<profomaOption>();

            return builder
                .Field(nameof(Contact))
            .Field(nameof(Feedback))
            .Field("AccrualRateType")
                .Build();



        }

    }
}