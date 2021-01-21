using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

namespace StateMachineGithubAction
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var arn = System.Environment.GetEnvironmentVariable("INPUT_STATEMACHINEARN") ??
                      "arn:aws:states:us-east-1:547578534168:stateMachine:RunEKSJob";
            var input = System.Environment.GetEnvironmentVariable("INPUT_PAYLOAD") ??
                        "{}";

            var rsp = await Run(arn, Guid.NewGuid().ToString(), input);
            try
            {
                await Wait(rsp.ExecutionArn, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                System.Environment.Exit(1);
            }
        }

        static Task<StartExecutionResponse> Run(string arn, string name, string input)
        {
            var client = new Amazon.StepFunctions.AmazonStepFunctionsClient();
            return client.StartExecutionAsync(new StartExecutionRequest()
                {
                Input = input,
                Name = name,
                StateMachineArn = arn
                }
            );
        }

        static async Task<string> Wait(string arn, TimeSpan wait, TimeSpan max)
        {
            var startTime = DateTimeOffset.Now;
            var client = new Amazon.StepFunctions.AmazonStepFunctionsClient();
            bool shouldContinue = true;
            
                while (shouldContinue)
                {
                    var result = await client.DescribeExecutionAsync(new DescribeExecutionRequest()
                    {
                        ExecutionArn = arn
                    });
                    Console.WriteLine(result.Status.Value);
                if (result.Status == ExecutionStatus.SUCCEEDED)
                    {
                        return result.Output;
                    }
                    else if (result.Status == ExecutionStatus.RUNNING)
                    {
                        if (DateTimeOffset.Now > startTime.Add(max))
                        {
                            //timed out 
                            throw new Exception("The state machine retried the max time");
                        }

                        await Task.Delay(wait);
                    }
                    else if (result.Status == ExecutionStatus.ABORTED)
                    {
                        throw new Exception("State machine aborted ");
                    }
                    else if (result.Status == ExecutionStatus.FAILED)
                    {
                        var history = await client.GetExecutionHistoryAsync(new GetExecutionHistoryRequest()
                        {
                            ExecutionArn = arn,
                            ReverseOrder = true,
                            IncludeExecutionData = true
                        });
                        var reason = history.Events
                            .FirstOrDefault(x => x.ExecutionFailedEventDetails != null)?.ExecutionFailedEventDetails
                            .Cause ?? "Unknown";
                            
                        throw new Exception("State machine failed. Reason: " + reason);
                    }
                    else if (result.Status == ExecutionStatus.TIMED_OUT)
                    {
                        throw new Exception("State machine timed out ");

                    }
                }
            return string.Empty;
        }
    }
}
