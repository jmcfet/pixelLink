
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DynamicOanels
{
    class MainVM
    {
        // Command Property to Bind to from View (done in xaml)
        //</summary
        public ICommand ButtonClickCommand { get; private set; }
        public MainVM()
        {
    //        ButtonClickCommand = new RelayCommand(PerformClickAction, CanClickButtonandPerformAction);
        }
        public void CanClickButtonandPerformAction(object parameter)
        {
            // Add any logic here to enable / disable the buttonreturntrue;
        }

        public void PerformClickAction(object parameter)
        {
            // Here you would put code to execute the desired command.  We are
            // going to show a message box.            System.Windows.MessageBox.Show("You are using Command Binding with WPF!!!");
        }
    }
}


