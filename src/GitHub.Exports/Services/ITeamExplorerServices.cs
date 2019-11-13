using System.Threading.Tasks;

namespace GitHub.Services
{
    public interface ITeamExplorerServices : INotificationService
    {
        void ShowConnectPage();
        void ShowCommitDetails(string oid);
        void ShowHomePage();
        void ShowPublishSection();
        Task ShowRepositorySettingsRemotesAsync();
        void ClearNotifications();
        void OpenRepository(string repositoryPath);
    }
}