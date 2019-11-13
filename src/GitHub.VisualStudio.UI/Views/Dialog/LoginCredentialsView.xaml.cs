﻿using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using GitHub.Controls;
using GitHub.Exports;
using GitHub.Extensions;
using GitHub.Services;
using GitHub.UI;
using GitHub.ViewModels.Dialog;
using ReactiveUI;

namespace GitHub.VisualStudio.Views.Dialog
{
    public class GenericLoginCredentialsView : ViewBase<ILoginCredentialsViewModel, LoginCredentialsView>
    { }

    /// <summary>
    /// Interaction logic for LoginControl.xaml
    /// </summary>
    [ExportViewFor(typeof(ILoginCredentialsViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class LoginCredentialsView : GenericLoginCredentialsView
    {
        public LoginCredentialsView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                SetupDotComBindings(d);
                SetupEnterpriseBindings(d);
                SetupSelectedTabBindings(d);
                d(Disposable.Create(Deactivate));
            });

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                    dotComUserNameOrEmail.TryMoveFocus(FocusNavigationDirection.First).Subscribe();
            };

            // Refocus VS after a SSO login attempt.
            this.WhenAnyObservable(
                x => x.ViewModel.GitHubLogin.LoginViaOAuth,
                x => x.ViewModel.EnterpriseLogin.LoginViaOAuth)
                .Subscribe(_ => SetForegroundWindow());

            hostTabControl.SelectionChanged += (s, e) =>
            {
                foreach (var i in e.RemovedItems)
                {
                    if (i == dotComTab)
                    {
                        ViewModel?.GitHubLogin.Deactivated();
                    }
                    else if (i == enterpriseTab)
                    {
                        ViewModel?.EnterpriseLogin.Deactivated();
                    }
                }
            };
        }

        void Deactivate()
        {
            ViewModel?.GitHubLogin.Deactivated();
            ViewModel?.EnterpriseLogin.Deactivated();
        }

        void SetupDotComBindings(Action<IDisposable> d)
        {
            d(this.OneWayBind(ViewModel, vm => vm.GitHubLogin.IsLoggingIn, x => x.dotComloginControlsPanel.IsEnabled, x => x == false));

            d(this.Bind(ViewModel, vm => vm.GitHubLogin.UsernameOrEmail, x => x.dotComUserNameOrEmail.Text));
            d(this.OneWayBind(ViewModel, vm => vm.GitHubLogin.UsernameOrEmailValidator, v => v.dotComUserNameOrEmailValidationMessage.ReactiveValidator));

            d(this.BindPassword(ViewModel, vm => vm.GitHubLogin.Password, v => v.dotComPassword.Text, dotComPassword));
            d(this.OneWayBind(ViewModel, vm => vm.GitHubLogin.PasswordValidator, v => v.dotComPasswordValidationMessage.ReactiveValidator));

            d(this.OneWayBind(ViewModel, vm => vm.GitHubLogin.Login, v => v.dotComLogInButton.Command));
            d(this.OneWayBind(ViewModel, vm => vm.GitHubLogin.LoginViaOAuth, v => v.dotComSsaLogInButton.Command));
            d(this.OneWayBind(ViewModel, vm => vm.GitHubLogin.NavigatePricing, v => v.pricingLink.Command));
            d(this.OneWayBind(ViewModel, vm => vm.GitHubLogin.Error, v => v.dotComErrorMessage.UserError));
        }

        void SetupEnterpriseBindings(Action<IDisposable> d)
        {
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.IsLoggingIn, x => x.enterpriseloginControlsPanel.IsEnabled, x => x == false));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.ProbeStatus, x => x.enterpriseUrl.IconContent));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.SupportedLoginMethods, x => x.enterpriseUsernamePasswordPanel.Visibility, x => x.HasValue && ((x & EnterpriseLoginMethods.UsernameAndPassword) != 0) ? Visibility.Visible : Visibility.Collapsed));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.SupportedLoginMethods, x => x.enterpriseTokenPanel.Visibility, x => x.HasValue && ((x & EnterpriseLoginMethods.Token) != 0) ? Visibility.Visible : Visibility.Collapsed));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.SupportedLoginMethods, x => x.enterpriseSsoPanel.Visibility, x => x.HasValue && ((x & EnterpriseLoginMethods.OAuth) != 0) ? Visibility.Visible : Visibility.Collapsed));

            d(this.Bind(ViewModel, vm => vm.EnterpriseLogin.UsernameOrEmail, x => x.enterpriseUserNameOrEmail.Text));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.UsernameOrEmailValidator, v => v.enterpriseUserNameOrEmailValidationMessage.ReactiveValidator));

            d(this.BindPassword(ViewModel, vm => vm.EnterpriseLogin.Password, v => v.enterprisePassword.Text, enterprisePassword));
            d(this.BindPassword(ViewModel, vm => vm.EnterpriseLogin.Password, v => v.enterpriseToken.Text, enterpriseToken));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.PasswordValidator, v => v.enterprisePasswordValidationMessage.ReactiveValidator));

            d(this.Bind(ViewModel, vm => vm.EnterpriseLogin.EnterpriseUrl, v => v.enterpriseUrl.Text));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.EnterpriseUrlValidator, v => v.enterpriseUrlValidationMessage.ReactiveValidator));

            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.Login, v => v.enterpriseLogInButton.Command));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.LoginViaOAuth, v => v.enterpriseSsaLogInButton.Command));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.NavigateLearnMore, v => v.learnMoreLink.Command));
            d(this.OneWayBind(ViewModel, vm => vm.EnterpriseLogin.Error, v => v.enterpriseErrorMessage.UserError));
        }

        void SetupSelectedTabBindings(Action<IDisposable> d)
        {
            d(this.WhenAny(x => x.ViewModel.LoginMode, x => x.Value)
                .Select(x => x == LoginMode.DotComOrEnterprise || x == LoginMode.DotComOnly)
                .Where(x => x == true)
                .BindTo(this, v => v.dotComTab.IsSelected));

            d(this.WhenAny(x => x.ViewModel.LoginMode, x => x.Value)
                .Select(x => x == LoginMode.EnterpriseOnly)
                .Where(x => x == true)
                .BindTo(this, v => v.enterpriseTab.IsSelected));
        }

        static bool SetForegroundWindow()
        {
            var hWnd = Process.GetCurrentProcess().MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                return NativeMethods.SetForegroundWindow(hWnd);
            }

            return false;
        }

        class NativeMethods
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetForegroundWindow(IntPtr hWnd);
        }
    }
}
