﻿using System;
using System.Reactive;
using GitHub.Models;
using GitHub.Primitives;
using ReactiveUI;

namespace GitHub.ViewModels.Dialog.Clone
{
    /// <summary>
    /// ViewModel for the Clone Repository dialog
    /// </summary>
    public interface IRepositoryCloneViewModel : IDialogContentViewModel, IConnectionInitializedViewModel
    {
        /// <summary>
        /// Gets the view model for the GitHub.com tab.
        /// </summary>
        IRepositorySelectViewModel GitHubTab { get; }

        /// <summary>
        /// Gets the view model for the enterprise tab.
        /// </summary>
        IRepositorySelectViewModel EnterpriseTab { get; }

        /// <summary>
        /// Initial URL for the dialog.
        /// </summary>
        UriString Url { get; set; }

        /// <summary>
        /// Gets the path to clone the repository to.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Gets a warning message that explains why <see cref="Path"/> is suspect.
        /// </summary>
        string PathWarning { get; }

        /// <summary>
        /// Gets the index of the selected tab.
        /// </summary>
        /// <remarks>
        /// The tabs are: GitHubPage, EnterprisePage, UrlPage.
        /// </remarks>
        int SelectedTabIndex { get; }

        /// <summary>
        /// Gets the command executed when the user clicks "Browse".
        /// </summary>
        ReactiveCommand<Unit, Unit> Browse { get; }

        /// <summary>
        /// Gets the command executed when the user clicks "Clone".
        /// </summary>
        ReactiveCommand<Unit, CloneDialogResult> Clone { get; }

        ReactiveCommand<Unit, Unit> LoginAsDifferentUser { get; }
    }
}
