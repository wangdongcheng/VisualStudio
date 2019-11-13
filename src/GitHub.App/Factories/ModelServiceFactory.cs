﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Api;
using GitHub.Caches;
using GitHub.Models;
using GitHub.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace GitHub.Factories
{
    [Export(typeof(IModelServiceFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ModelServiceFactory : IModelServiceFactory, IDisposable
    {
        readonly IApiClientFactory apiClientFactory;
        readonly IHostCacheFactory hostCacheFactory;
        readonly IAvatarProvider avatarProvider;
        readonly Dictionary<IConnection, ModelService> cache = new Dictionary<IConnection, ModelService>();
        readonly SemaphoreSlim cacheLock = new SemaphoreSlim(1);

        [ImportingConstructor]
        public ModelServiceFactory(
            IApiClientFactory apiClientFactory,
            IHostCacheFactory hostCacheFactory,
            IAvatarProvider avatarProvider,
            [Import(AllowDefault = true)] JoinableTaskContext joinableTaskContext)
        {
            this.apiClientFactory = apiClientFactory;
            this.hostCacheFactory = hostCacheFactory;
            this.avatarProvider = avatarProvider;
            JoinableTaskContext = joinableTaskContext ?? ThreadHelper.JoinableTaskContext;
        }

        public async Task<IModelService> CreateAsync(IConnection connection)
        {
            ModelService result;

            await cacheLock.WaitAsync();

            try
            {
                if (!cache.TryGetValue(connection, out result))
                {
                    result = new ModelService(
                        await apiClientFactory.Create(connection.HostAddress),
                        await hostCacheFactory.Create(connection.HostAddress),
                        avatarProvider);
                    result.InsertUser(AccountCacheItem.Create(connection.User));
                    cache.Add(connection, result);
                }
            }
            finally
            {
                cacheLock.Release();
            }

            return result;
        }

        public IModelService CreateBlocking(IConnection connection)
        {
            return JoinableTaskContext.Factory.Run(() => CreateAsync(connection));
        }

        public void Dispose() => cacheLock.Dispose();

        JoinableTaskContext JoinableTaskContext { get; }
    }
}
