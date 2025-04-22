using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace survival_list_overlay.Models
{
    public class TrackedItem : INotifyPropertyChanged
    {
        private int progress;
        private string progressStatus = "not_tracked";
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
                    UpdateStatus();

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
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<TrackedItem> CompletionCallback;

        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private void UpdateStatus()
        {
            if (Progress >= Total)
                ProgressStatus = "completed";
            else
                ProgressStatus = "unfinished";

            
                
        }
        }

    }

