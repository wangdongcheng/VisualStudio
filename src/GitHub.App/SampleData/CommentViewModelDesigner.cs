﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Threading.Tasks;
using GitHub.Models;
using GitHub.Services;
using GitHub.ViewModels;
using ReactiveUI;

namespace GitHub.SampleData
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    public class CommentViewModelDesigner : ReactiveObject, ICommentViewModel
    {
        public CommentViewModelDesigner()
        {
            Author = new ActorViewModel { Login = "shana" };
        }

        public string Id { get; set; }
        public int PullRequestId { get; set; }
        public int DatabaseId { get; set; }
        public string Body { get; set; }
        public string ErrorMessage { get; set; }
        public CommentEditState EditState { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsSubmitting { get; set; }
        public bool CanCancel { get; } = true;
        public bool CanDelete { get; } = true;
        public string CommitCaption { get; set; } = "Comment";
        public ICommentThreadViewModel Thread { get; }
        public DateTimeOffset CreatedAt => DateTime.Now.Subtract(TimeSpan.FromDays(3));
        public IActorViewModel Author { get; set; }
        public Uri WebUrl { get; }

        public ReactiveCommand<Unit, Unit> BeginEdit { get; }
        public ReactiveCommand<Unit, Unit> CancelEdit { get; }
        public ReactiveCommand<Unit, Unit> CommitEdit { get; }
        public ReactiveCommand<Unit, Unit> OpenOnGitHub { get; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> Delete { get; }
        public IAutoCompleteAdvisor AutoCompleteAdvisor { get; }

        public Task InitializeAsync(ICommentThreadViewModel thread, ActorModel currentUser, CommentModel comment, CommentEditState state)
        {
            return Task.CompletedTask;
        }
    }
}
