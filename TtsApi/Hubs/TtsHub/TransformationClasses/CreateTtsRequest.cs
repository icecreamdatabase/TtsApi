using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Polly;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Aws;
using TtsApi.Hubs.TtsHub.TransferClasses;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses;

public class CreateTtsRequest
{
    private readonly ILogger<CreateTtsRequest> _logger;
    private readonly Polly _polly;

    public CreateTtsRequest(ILogger<CreateTtsRequest> logger, Polly polly)
    {
        _logger = logger;
        _polly = polly;
    }

    public async Task<TtsRequest> GetTtsRequest(RequestQueueIngest rqi)
    {
        List<Task<TtsIndividualSynthesize>> tasks = TtsHandlerStatics.SplitMessage(rqi)
            .Select(part => GenerateIndividualSynthesizeTask(rqi, part)
                .ContinueWith(task => UpdateCharacterCostWithoutDbSave(rqi, task))
            )
            .ToList();

        TtsIndividualSynthesize[] ttsIndividualSynthesizes = await Task.WhenAll(tasks);

        return new()
        {
            RedemptionId = rqi.RedemptionId,
            MaxMessageTimeSeconds = rqi.Reward.Channel.MaxMessageTimeSeconds,
            TtsIndividualSynthesizes = ttsIndividualSynthesizes.ToList()
        };
    }

    private Task<TtsIndividualSynthesize> GenerateIndividualSynthesizeTask(RequestQueueIngest rqi,
        TtsMessagePart part)
    {
        try
        {
            return TtsIndividualSynthesize.ParseFromSynthesizeTasks(
                _polly.Synthesize(part.Message, part.VoiceId, part.Engine),
                rqi.Reward.RequestVisme
                    ? _polly.SpeechMarks(part.Message, part.VoiceId, part.Engine)
                    : null,
                part
            );
        }
        catch (AmazonPollyException e)
        {
            _logger.LogWarning("GetTtsRequest error: {Message}", e.Message);
            return Task.FromResult(new TtsIndividualSynthesize());
        }
    }

    private static TtsIndividualSynthesize UpdateCharacterCostWithoutDbSave(RequestQueueIngest rqi,
        Task<TtsIndividualSynthesize> task)
    {
        TtsIndividualSynthesize individualSynthesize = task.GetAwaiter().GetResult();
        if (individualSynthesize.TtsMessagePart.Engine == Engine.Standard)
            rqi.CharacterCostStandard = rqi.CharacterCostStandard.GetValueOrDefault(0) +
                                        individualSynthesize.RequestCharacters;
        else
            rqi.CharacterCostNeural = rqi.CharacterCostNeural.GetValueOrDefault(0) +
                                      individualSynthesize.RequestCharacters;
        return individualSynthesize;
    }
}
