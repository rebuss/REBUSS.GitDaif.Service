﻿namespace AzureDevOpsPullRequestAPI.Agents
{
    public interface InterfaceAI
    {
        Task<object> AskAgent(string prompt, string filePath = null);
    }
}
