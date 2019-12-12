using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElfChain.Common.DtoExtension;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Automation.ApiTest
{
    public partial class ChainApiTests
    {
        public ChainApiTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _client = AElfClientExtension.GetClient(ServiceUrl);
            _listener = new AnalyzeListener(testOutputHelper);
        }

        private readonly ITestOutputHelper _testOutputHelper;
        private const string ServiceUrl = "192.168.197.15:8100";
        private readonly AElfClient _client;
        private readonly AnalyzeListener _listener;

        [Fact]
        public async Task GetBlock_Test()
        {
            long totalCount = 0;

            var hashList = await GetBlockByHeight_Test();
            foreach (var hash in hashList)
            {
                var (block, timeSpan) = await _listener.ExecuteApi(o => _client.GetBlockByHashAsync(hash));
                block.BlockHash.ShouldBe(hash);
                totalCount += timeSpan;
                _testOutputHelper.WriteLine($"block hash: {hash}, execute time: {timeSpan}ms");
            }

            _testOutputHelper.WriteLine(
                $"Total blocks:{hashList.Count}, Total time span: {totalCount}ms, Average time: {totalCount / hashList.Count}ms");
        }

        [Fact]
        public async Task<List<string>> GetBlockByHeight_Test()
        {
            var chainStatus = await _client.GetChainStatusAsync();
            var libHeight = chainStatus.LastIrreversibleBlockHeight;
            var longestHeight = chainStatus.LongestChainHeight;
            var hashList = new List<string>();
            long totalCount = 0;
            for (var i = libHeight; i <= longestHeight; i++)
            {
                var (block, timeSpan) = await _listener.ExecuteApi(o => _client.GetBlockByHeightAsync(i));
                hashList.Add(block.BlockHash);
                totalCount += timeSpan;
                _testOutputHelper.WriteLine($"block height: {i}, execute time: {timeSpan}ms");
            }

            var totalBlocks = longestHeight - libHeight + 1;
            _testOutputHelper.WriteLine(
                $"Total blocks:{totalBlocks}, Total time span: {totalCount}ms, Average time: {totalCount / totalBlocks}ms");

            return hashList;
        }

        [Fact]
        public async Task GetBlockHeight_Test()
        {
            var (result, timeSpan) = await _listener.ExecuteApi(_client.GetBlockHeightAsync);
            result.ShouldBeGreaterThan(1);

            _testOutputHelper.WriteLine($"block height: {result.ToString()}, execute time: {timeSpan}ms");
        }

        [Fact]
        public async Task GetBlockState_Test()
        {
            var (chainStatus, timeSpan) = await _listener.ExecuteApi(_client.GetChainStatusAsync);
            _testOutputHelper.WriteLine($"Chain status info: {chainStatus}, execute time: {timeSpan}ms");
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GetCurrentRoundInformation_Test()
        {
            long totalCount = 0;
            for (var i = 0; i < 100; i++)
            {
                var (roundInfo, timeSpan) = await _listener.ExecuteApi(_client.GetCurrentRoundInformationAsync);
                totalCount += timeSpan;
                _testOutputHelper.WriteLine($"Round info: {roundInfo.RoundNumber}, execute time: {timeSpan}ms");
                Thread.Sleep(50);
            }

            _testOutputHelper.WriteLine($"Total time: {totalCount}ms, Average time: {totalCount / 100}ms");
        }
    }
}