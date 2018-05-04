﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Specialized;


namespace ExpMngr
{
    /// <summary>
    /// The base unit of experiments. A Trial is usually a singular attempt at a task by a participant after/during the presentation of a stimulus.
    /// </summary>
    [Serializable]
    public class Trial {

        /// <summary>
        /// Returns non-zero indexed trial number.
        /// </summary>
        public int number { get { return session.trials.ToList().IndexOf(this) + 1; } }
        /// <summary>
        /// Returns non-zero indexed trial number for the current block.
        /// </summary>
        public int numberInBlock { get { return block.trials.IndexOf(this) + 1; } }
        /// <summary>
        /// Status of the trial (enum)
        /// </summary>
        public TrialStatus status = TrialStatus.NotDone;
        /// <summary>
        ///  The block the trial belongs to
        /// </summary>
        [NonSerialized] public Block block;
        float startTime, endTime;
        protected ExperimentSession session;
        
        /// <summary>
        /// Trial settings. These will override block settings if set.
        /// </summary>
        public Settings settings = Settings.empty;

        /// <summary>
        /// Ordered dictionary of results in a order.
        /// </summary>
        public OrderedResultDict result;

        /// <summary>
        /// Create a trial. You then need to add this trial to a block with block.trials.Add(...)
        /// </summary>
        internal Trial()
        {
            
        }

        /// <summary>
        /// Set references for the trial.
        /// </summary>
        /// <param name="trialBlock">The block the trial belongs to.</param>
        internal void SetReferences(Block trialBlock)
        {
            block = trialBlock;
            session = block.session;
            settings.SetParent(block.settings);
        }

        /// <summary>
        /// Begins the trial, updating the current trial and block number, setting the status to in progress, starting the timer for the trial, and beginning recording positions of every object with an attached tracker
        /// </summary>
        public void Begin()
        {
            session.currentTrialNum = number;
            session.currentBlockNum = block.number;

            status = TrialStatus.InProgress;
            startTime = Time.time;
            result = new OrderedResultDict();
            foreach (string h in session.headers)
                result.Add(h, string.Empty);

            result["ppid"] = session.ppid;
            result["session_num"] = session.sessionNum;
            result["trial_num"] = number;
            result["block_num"] = block.number;
            result["trial_num_in_block"] = numberInBlock;
            result["start_time"] = startTime;

            foreach (Tracker tracker in session.trackedObjects)
            {
                tracker.StartRecording();
            }
            session.onTrialBegin.Invoke(this);
        }

        /// <summary>
        /// Ends the Trial, queues up saving results to output file, stops and saves tracked object data.
        /// </summary>
        public void End()
        {
            status = TrialStatus.Done;
            endTime = Time.time;
            result["end_time"] = endTime;            

            // log tracked objects
            foreach (Tracker tracker in session.trackedObjects)
            {
                var trackingData = tracker.StopRecording();
                string dataName = session.SaveTrackingData(tracker.objectName, trackingData);
                result[tracker.objectNameHeader] = dataName;
            }

            // log any settings we need to for this trial
            foreach (string s in session.settingsToLog)
            {
                result[s] = settings[s];
            }
            session.onTrialEnd.Invoke(this);
        }


        [Obsolete("This constructor is obsolete, please create blocks and trials with the session.")]
        /// <summary>
        /// Create trial with an associated block
        /// </summary>
        /// <param name="trialBlock">The block the trial belongs to.</param>
        public Trial(Block trialBlock)
        {
            block = trialBlock;
            block.trials.Add(this);
            session = block.session;
            settings.SetParent(block.settings);
        }


    }

    

    /// <summary>
    /// Status of a trial
    /// </summary>
    public enum TrialStatus { NotDone, InProgress, Done }


}