﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using GitHub.Extensions;
using GitHub.Models;
using GitHub.Primitives;
using GitHub.Services;
using GitHub.ViewModels.Dialog.Clone;
using LibGit2Sharp;
using NSubstitute;
using NUnit.Framework;
using Rothko;

namespace GitHub.App.UnitTests.ViewModels.Dialog.Clone
{
    public class RepositoryCloneViewModelTests
    {
        const string directoryEmpty = "d:\\empty\\directory";
        const string directoryExists = "d:\\exists\\directory";
        const string fileExists = "d:\\exists\\file";
        const string defaultPath = "d:\\default\\path";

        public class TheUrlProperty
        {
            [TestCase("https://github.com;https://enterprise.com", null, 0)]
            [TestCase("https://github.com;https://enterprise.com", "https://github.com/foo/bar", 0)]
            [TestCase("https://github.com;https://enterprise.com", "https://enterprise.com/foo/bar", 1)]
            [TestCase("https://github.com;https://enterprise.com", "HTTPS://ENTERPRISE.COM/FOO/BAR", 1)]
            [TestCase("https://github.com;https://enterprise.com", "https://unknown.com/foo/bar", 0)]
            public async Task Select_Tab_For_Url(string addresses, string url, int expectTabIndex)
            {
                var cm = CreateConnectionManager(addresses.Split(';'));
                var target = CreateTarget(connectionManager: cm);
                target.Url = url;

                await target.InitializeAsync(null);

                Assert.That(target.SelectedTabIndex, Is.EqualTo(expectTabIndex));
            }

            [TestCase("https://github.com;https://enterprise.com", null, "", "")]
            [TestCase("https://github.com;https://enterprise.com", "https://github.com/foo/bar", "https://github.com/foo/bar", "")]
            [TestCase("https://github.com;https://enterprise.com", "https://enterprise.com/foo/bar", "", "https://enterprise.com/foo/bar")]
            [TestCase("https://github.com;https://enterprise.com", "HTTPS://GITHUB.COM/FOO/BAR", "HTTPS://GITHUB.COM/FOO/BAR", "")]
            [TestCase("https://github.com;https://enterprise.com", "HTTPS://ENTERPRISE.COM/FOO/BAR", "", "HTTPS://ENTERPRISE.COM/FOO/BAR")]
            [TestCase("https://github.com;https://enterprise.com", "https://unknown.com/foo/bar", "", "")]
            public async Task Set_Filter_For_Url(string addresses, string url, string expectGitHubFilter, string expectEnterpriseFilter)
            {
                var cm = CreateConnectionManager(addresses.Split(';'));
                var target = CreateTarget(connectionManager: cm);
                target.Url = url;

                await target.InitializeAsync(null);

                Assert.That(target.GitHubTab.Filter, Is.EqualTo(expectGitHubFilter));
                Assert.That(target.EnterpriseTab.Filter, Is.EqualTo(expectEnterpriseFilter));
            }
        }

        [Test]
        public async Task GitHubPage_Is_Initialized()
        {
            var cm = CreateConnectionManager("https://github.com");
            var target = CreateTarget(connectionManager: cm);

            await target.InitializeAsync(null);

            target.GitHubTab.Received(1).Initialize(cm.Connections[0]);
            target.EnterpriseTab.DidNotReceiveWithAnyArgs().Initialize(null);
        }

        [Test]
        public async Task EnterprisePage_Is_Initialized()
        {
            var cm = CreateConnectionManager("https://enterprise.com");
            var target = CreateTarget(connectionManager: cm);

            await target.InitializeAsync(null);

            target.GitHubTab.DidNotReceiveWithAnyArgs().Initialize(null);
            target.EnterpriseTab.Received(1).Initialize(cm.Connections[0]);
        }

        [Test]
        public async Task GitHub_And_Enterprise_Pages_Are_Initialized()
        {
            var cm = CreateConnectionManager("https://github.com", "https://enterprise.com");
            var target = CreateTarget(connectionManager: cm);

            await target.InitializeAsync(null);

            target.GitHubTab.Received(1).Initialize(cm.Connections[0]);
            target.EnterpriseTab.Received(1).Initialize(cm.Connections[1]);
        }

        [Test]
        public async Task GitHubPage_Is_Loaded()
        {
            var cm = CreateConnectionManager("https://github.com", "https://enterprise.com");
            var target = CreateTarget(connectionManager: cm);

            await target.InitializeAsync(cm.Connections[0]);

            await target.GitHubTab.Received(1).Activate();
            await target.EnterpriseTab.DidNotReceive().Activate();
        }

        [Test]
        public async Task Enterprise_Is_Loaded()
        {
            var cm = CreateConnectionManager("https://github.com", "https://enterprise.com");
            var target = CreateTarget(connectionManager: cm);

            await target.InitializeAsync(cm.Connections[1]);

            await target.GitHubTab.DidNotReceive().Activate();
            await target.EnterpriseTab.Received(1).Activate();
        }

        [Test]
        public async Task Switching_To_GitHubPage_Loads_It()
        {
            var cm = CreateConnectionManager("https://github.com", "https://enterprise.com");
            var target = CreateTarget(connectionManager: cm);

            await target.InitializeAsync(cm.Connections[1]);
            await target.GitHubTab.DidNotReceive().Activate();

            target.SelectedTabIndex = 0;

            await target.GitHubTab.Received(1).Activate();
        }

        [Test]
        public async Task Switching_To_EnterprisePage_Loads_It()
        {
            var cm = CreateConnectionManager("https://github.com", "https://enterprise.com");
            var target = CreateTarget(connectionManager: cm);

            await target.InitializeAsync(cm.Connections[0]);
            await target.EnterpriseTab.DidNotReceive().Activate();

            target.SelectedTabIndex = 1;

            await target.EnterpriseTab.Received(1).Activate();
        }

        [Test]
        public void Path_Is_Initialized()
        {
            var target = CreateTarget();

            Assert.That(target.Path, Is.EqualTo(defaultPath));
        }

        [Test]
        public void Owner_And_Repository_Name_Is_Appended_To_Base_Path()
        {
            var owner = "owner";
            var repo = "repo";
            var target = CreateTarget();
            var expectPath = Path.Combine(defaultPath, owner, repo);

            SetRepository(target.GitHubTab, CreateRepositoryModel(owner, repo));

            Assert.That(target.Path, Is.EqualTo(expectPath));
        }

        [Test]
        public void PathWarning_Is_Not_Set_When_No_Repository_Selected()
        {
            var target = CreateTarget();

            target.Path = directoryExists;

            Assert.That(target.PathWarning, Is.Null);
        }

        [Test]
        public void PathWarning_Is_Set_For_Existing_File_At_Destination()
        {
            var target = CreateTarget();
            SetRepository(target.GitHubTab, CreateRepositoryModel("owner", "repo"));
            target.Path = fileExists;

            Assert.That(target.PathWarning, Is.EqualTo(Resources.DestinationAlreadyExists));
        }

        [Test]
        public void PathWarning_Is_Set_For_Existing_Clone_At_Destination()
        {
            var owner = "owner";
            var repo = "repo";
            var remoteUrl = CreateGitHubUrl("owner", "repo");
            var gitService = CreateGitService(true, remoteUrl);
            var target = CreateTarget(gitService: gitService);
            SetRepository(target.GitHubTab, CreateRepositoryModel(owner, repo));
            target.Path = directoryExists;

            Assert.That(target.PathWarning, Is.EqualTo(Resources.YouHaveAlreadyClonedToThisLocation));
        }

        [Test]
        public void PathWarning_Is_Set_For_Repository_With_No_Origin()
        {
            var owner = "owner";
            var repo = "repo";
            var gitService = CreateGitService(true, null);
            var target = CreateTarget(gitService: gitService);
            SetRepository(target.GitHubTab, CreateRepositoryModel(owner, repo));
            target.Path = directoryExists;

            Assert.That(target.PathWarning, Is.EqualTo(Resources.LocalRepositoryDoesntHaveARemoteOrigin));
        }

        [Test]
        public void PathWarning_Is_Set_For_Directory_With_No_Repository()
        {
            var owner = "owner";
            var repo = "repo";
            var gitService = CreateGitService(false, null);
            var target = CreateTarget(gitService: gitService);
            SetRepository(target.GitHubTab, CreateRepositoryModel(owner, repo));
            target.Path = directoryExists;

            Assert.That(target.PathWarning, Is.EqualTo(Resources.DirectoryAtDestinationNotEmpty));
        }

        [Test]
        public void PathWarning_Is_Set_For_Existing_Repository_At_Destination_With_Different_Remote()
        {
            var originalOwner = "original_Owner";
            var forkedOwner = "forked_owner";
            var repo = "repo";
            var forkedUrl = CreateGitHubUrl(forkedOwner, repo);
            var expectMessage = string.Format(CultureInfo.CurrentCulture, Resources.LocalRepositoryHasARemoteOf, forkedUrl);
            var gitService = CreateGitService(true, CreateGitHubUrl(forkedOwner, repo));
            var target = CreateTarget(gitService: gitService);
            SetRepository(target.GitHubTab, CreateRepositoryModel(originalOwner, repo));

            target.Path = directoryExists;

            Assert.That(target.PathWarning, Is.EqualTo(expectMessage));
        }

        [Test]
        public void PathWarning_Is_Not_Set_When_EmptyDirectoryExists_Selected()
        {
            var target = CreateTarget();

            target.Path = directoryEmpty;

            Assert.That(target.PathWarning, Is.Null);
        }

        [Test]
        public void Repository_Name_Replaces_Last_Part_Of_Non_Base_Path()
        {
            var target = CreateTarget();

            var owner = "owner";
            target.Path = "d:\\efault";
            SetRepository(target.GitHubTab, CreateRepositoryModel(owner, "name"));
            target.Path = $"d:\\efault\\{owner}\\foo";
            SetRepository(target.GitHubTab, CreateRepositoryModel(owner, "repo"));

            Assert.That(target.Path, Is.EqualTo($"d:\\efault\\{owner}\\repo"));
        }

        [TestCase("c:\\base", "owner1/repo1", "c:\\base\\owner1\\repo1", "owner2/repo2", "c:\\base\\owner2\\repo2",
            Description = "Path unchanged")]
        [TestCase("c:\\base", "owner1/repo1", "c:\\base\\owner1\\changed", "owner2/repo2", "c:\\base\\owner2\\repo2",
            Description = "Repo name changed")]
        [TestCase("c:\\base", "owner1/repo1", "c:\\base\\owner1", "owner2/repo2", "c:\\base\\owner2\\repo2",
            Description = "Repo name deleted")]
        [TestCase("c:\\base", "owner1/repo1", "c:\\base", "owner2/repo2", "c:\\base\\owner2\\repo2",
            Description = "Base path reverted")]

        [TestCase("c:\\base", "owner1/repo1", "c:\\new\\base\\owner1\\changed", "owner2/repo2", "c:\\new\\base\\owner2\\repo2",
            Description = "Base path and repo name changed")]
        [TestCase("c:\\base", "owner1/repo1", "c:\\new\\base\\owner1", "owner2/repo2", "c:\\new\\base\\owner2\\repo2",
            Description = "Base path changed and repo name deleted")]
        [TestCase("c:\\base", "owner1/repo1", "c:\\new\\base", "owner2/repo2", "c:\\new\\base\\owner2\\repo2",
            Description = "Base path changed and repo owner/name deleted")]

        [TestCase("c:\\base", "owner1/repo1", "", "owner2/repo2", "c:\\base\\owner2\\repo2",
            Description = "Base path cleared")]
        [TestCase("c:\\base", "owner1/repo1", "c:\\base\\repo1", "owner2/repo2", "c:\\base\\owner2\\repo2",
            Description = "Owner deleted")]
        [TestCase("c:\\base", "same/same", "c:\\base\\same\\same", "owner2/repo2", "c:\\base\\owner2\\repo2",
            Description = "Owner and repo have same name")]
        public void User_Edits_Path(string defaultClonePath, string repo1, string userPath, string repo2, string expectPath)
        {
            var target = CreateTarget(defaultClonePath: defaultClonePath);
            SetRepository(target.GitHubTab, CreateRepositoryModel(repo1));
            target.Path = userPath;

            SetRepository(target.GitHubTab, CreateRepositoryModel(repo2));

            Assert.That(target.Path, Is.EqualTo(expectPath));
        }

        [Test]
        public async Task Clone_Is_Initially_Disabled()
        {
            var target = CreateTarget();

            await target.InitializeAsync(null);

            Assert.That(target.Clone.CanExecute(null), Is.False);
        }

        [Test]
        public async Task Clone_Is_Enabled_When_Repository_Selected()
        {
            var target = CreateTarget();

            await target.InitializeAsync(null);

            SetRepository(target.GitHubTab, CreateRepositoryModel());

            Assert.That(target.Clone.CanExecute(null), Is.True);
        }

        [Test]
        public async Task Clone_Is_Disabled_When_Path_DirectoryExists()
        {
            var target = CreateTarget();

            await target.InitializeAsync(null);

            SetRepository(target.GitHubTab, CreateRepositoryModel());
            Assert.That(target.Clone.CanExecute(null), Is.True);

            target.Path = directoryExists;

            Assert.That(target.Clone.CanExecute(null), Is.False);
        }

        [Test]
        public async Task Open_Is_Enabled_When_Path_DirectoryExists()
        {
            var target = CreateTarget();

            await target.InitializeAsync(null);
            Assert.That(target.Open.CanExecute(null), Is.False);
            SetRepository(target.GitHubTab, CreateRepositoryModel());

            target.Path = directoryExists;

            Assert.That(target.Open.CanExecute(null), Is.True);
        }

        [Test]
        public async Task Clone_Is_Enabled_When_Path_EmptyDirectoryExists()
        {
            var target = CreateTarget();

            await target.InitializeAsync(null);

            SetRepository(target.GitHubTab, CreateRepositoryModel());
            Assert.That(target.Clone.CanExecute(null), Is.True);

            target.Path = directoryEmpty;

            Assert.That(target.Clone.CanExecute(null), Is.True);
        }

        static void SetRepository(IRepositoryCloneTabViewModel vm, RepositoryModel repository)
        {
            vm.Repository.Returns(repository);
            vm.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
                vm,
                new PropertyChangedEventArgs(nameof(vm.Repository)));
        }

        static IConnectionManager CreateConnectionManager(params string[] addresses)
        {
            var result = Substitute.For<IConnectionManager>();
            var connections = new ObservableCollectionEx<IConnection>();

            result.Connections.Returns(connections);
            result.GetLoadedConnections().Returns(connections);
            result.GetConnection(null).ReturnsForAnyArgs(default(IConnection));

            foreach (var address in addresses)
            {
                var connection = Substitute.For<IConnection>();
                var hostAddress = HostAddress.Create(address);
                connection.HostAddress.Returns(hostAddress);
                connection.IsLoggedIn.Returns(true);
                connections.Add(connection);
                result.GetConnection(hostAddress).Returns(connection);
            }

            return result;
        }

        static IRepositorySelectViewModel CreateSelectViewModel()
        {
            var result = Substitute.For<IRepositorySelectViewModel>();
            result.Repository.Returns((RepositoryModel)null);
            result.WhenForAnyArgs(x => x.Initialize(null)).Do(_ => result.IsEnabled.Returns(true));
            return result;
        }

        static IRepositoryCloneService CreateRepositoryCloneService(string defaultClonePath)
        {
            var result = Substitute.For<IRepositoryCloneService>();
            result.DefaultClonePath.Returns(defaultClonePath);
            result.DestinationDirectoryExists(directoryExists).Returns(true);
            result.DestinationDirectoryExists(directoryEmpty).Returns(true);
            result.DestinationDirectoryEmpty(directoryEmpty).Returns(true);
            result.DestinationFileExists(directoryExists).Returns(false);
            result.DestinationDirectoryExists(fileExists).Returns(false);
            result.DestinationFileExists(fileExists).Returns(true);
            return result;
        }

        static RepositoryCloneViewModel CreateTarget(
            IOperatingSystem os = null,
            IConnectionManager connectionManager = null,
            IRepositoryCloneService service = null,
            IUsageTracker usageTracker = null,
            IDialogService dialogService = null,
            IRepositorySelectViewModel gitHubTab = null,
            IRepositorySelectViewModel enterpriseTab = null,
            IGitService gitService = null,
            string defaultClonePath = defaultPath)
        {
            os = os ?? Substitute.For<IOperatingSystem>();
            connectionManager = connectionManager ?? CreateConnectionManager("https://github.com");
            service = service ?? CreateRepositoryCloneService(defaultClonePath);
            usageTracker = usageTracker ?? Substitute.For<IUsageTracker>();
            dialogService = dialogService ?? Substitute.For<IDialogService>();
            gitHubTab = gitHubTab ?? CreateSelectViewModel();
            enterpriseTab = enterpriseTab ?? CreateSelectViewModel();
            gitService = gitService ?? CreateGitService(true, "https://github.com/owner/repo");

            return new RepositoryCloneViewModel(
                os,
                connectionManager,
                service,
                gitService,
                usageTracker,
                dialogService,
                gitHubTab,
                enterpriseTab);
        }

        private static IGitService CreateGitService(bool repositoryExists, UriString remoteUrl)
        {
            var gitService = Substitute.For<IGitService>();

            IRepository repository = null;
            if (repositoryExists)
            {
                repository = Substitute.For<IRepository>();
                gitService.GetRemoteUri(repository).Returns(remoteUrl);
            }

            gitService.GetRepository(directoryExists).Returns(repository);
            return gitService;
        }

        static RepositoryModel CreateRepositoryModel(string repo = "owner/repo")
        {
            var split = repo.Split('/');
            var (owner, name) = (split[0], split[1]);
            return CreateRepositoryModel(owner, name);
        }

        static RepositoryModel CreateRepositoryModel(string owner, string name)
        {
            var cloneUrl = CreateGitHubUrl(owner, name);
            return new RepositoryModel(name, cloneUrl);
        }

        static UriString CreateGitHubUrl(string owner, string repo)
        {
            return new UriString($"https://github.com/{owner}/{repo}");
        }
    }
}
