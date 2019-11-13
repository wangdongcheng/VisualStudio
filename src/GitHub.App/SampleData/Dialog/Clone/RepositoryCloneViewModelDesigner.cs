﻿using System;
using System.Reactive;
using System.Threading.Tasks;
using GitHub.Models;
using GitHub.Primitives;
using GitHub.ViewModels;
using GitHub.ViewModels.Dialog.Clone;
using ReactiveUI;

namespace GitHub.SampleData.Dialog.Clone
{
    public class RepositoryCloneViewModelDesigner : ViewModelBase, IRepositoryCloneViewModel
    {
        public RepositoryCloneViewModelDesigner()
        {
            GitHubTab = new SelectPageViewModelDesigner();
            EnterpriseTab = new SelectPageViewModelDesigner();
        }

        public string Path { get; set; }
        public UriString Url { get; set; }
        public string PathWarning { get; set; }
        public int SelectedTabIndex { get; set; }
        public string Title => null;
        public IObservable<object> Done => null;
        public IRepositorySelectViewModel GitHubTab { get; }
        public IRepositorySelectViewModel EnterpriseTab { get; }
        public ReactiveCommand<Unit, Unit> Browse { get; }
        public ReactiveCommand<Unit, CloneDialogResult> Clone { get; }
        public ReactiveCommand<Unit, Unit> LoginAsDifferentUser { get; }

        public Task InitializeAsync(IConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
