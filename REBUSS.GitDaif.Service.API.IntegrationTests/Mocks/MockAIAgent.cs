using REBUSS.GitDaif.Service.API.Agents;
using REBUSS.GitDaif.Service.API.DTO.Responses;

namespace REBUSS.GitDaif.Service.API.IntegrationTests.Mocks
{
    public class MockAIAgent : IAIAgent
    {
        private readonly string _mockResponse;
        private readonly bool _shouldSucceed;

        public MockAIAgent(string mockResponse = "Mock AI response", bool shouldSucceed = true)
        {
            _mockResponse = mockResponse;
            _shouldSucceed = shouldSucceed;
        }

        public Task<BaseResponse> AskAgent(string prompt, string filePath = null)
        {
            if (!_shouldSucceed)
            {
                throw new InvalidOperationException("Mock AI agent failed");
            }

            return Task.FromResult(new BaseResponse
            {
                Success = true,
                Message = _mockResponse,
                Timestamp = DateTime.Now
            });
        }
    }
}
