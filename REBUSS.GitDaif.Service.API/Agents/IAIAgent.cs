using REBUSS.GitDaif.Service.API.DTO.Responses;

namespace REBUSS.GitDaif.Service.API.Agents
{
    public interface IAIAgent
    {
        Task<BaseResponse> AskAgent(string prompt, string filePath = null);
    }
}
