using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.UI.MVVM.Command;


namespace survival_list_overlay.Models
{

    public class TrackedItem : INotifyPropertyChanged
    {

        //Var Properties
        private int progress;
        private string progressStatus = "unfinished";
        public string Name { get; set; }
        public int Total { get; set; }

        public int Progress
        {
            get => progress;
            set
            {
                if (progress != value)
                {
                    progress = value;
                    OnPropertyChanged(nameof(Progress));
                    ProgressStatus = (progress >= Total) ? "completed" : "unfinished";

                }
            }
        }
        public string ProgressStatus
        {
            get => progressStatus;
            private set
            {
                if (progressStatus != value)
                {
                    progressStatus = value;
                    OnPropertyChanged(nameof(ProgressStatus));
                    if (progressStatus == "completed")
                        CompletionCallback?.Invoke(this);
                }
            }
        }

        //Event-related
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<TrackedItem> CompletionCallback;

        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        /*private void UpdateStatus()
        {
            if (Progress >= Total)
                ProgressStatus = "completed";
            else
                ProgressStatus = "unfinished";

            
                
        }
        */
        
        //ICommands
        public ICommand IncrementCommand { get; }
        public ICommand DecrementCommand { get; }

        //Constructor
        public TrackedItem()
        {
            IncrementCommand = new RelayCommand(_ => Progress++,  _ => Progress < Total );
            DecrementCommand = new RelayCommand( _ => Progress--, _ => Progress > 0);
            
        }

    }
        

    }

