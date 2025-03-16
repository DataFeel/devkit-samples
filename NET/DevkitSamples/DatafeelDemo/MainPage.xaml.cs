using ReactiveUI.Maui;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Datafeel;

namespace DatafeelDemo
{
    public partial class MainPage : ReactiveContentPage<MainViewModel>
    {
        public MainPage(MainViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            this.WhenActivated(_ =>
            {
            });
        }
    }
}
