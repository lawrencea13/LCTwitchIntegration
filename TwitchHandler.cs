using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalChat
{
    internal class TwitchHandler
    {
        internal string Authorization {  private get; set; }
        internal string ChannelID { private get; set; }
        internal string ClientID { private get; set; }

        #region Poll Settings

        internal bool pollChannelPointVoting { private get; set; }
        internal int pollCostPerVote { private get; set; }
        internal int pollDuration { private get; set; }

        #endregion


        // store this here
        private List<string> CurrentPollIDs = new List<string>();

        // also store this here, if not empty, must be cleaned up on destroy
        private List<string> RewardIDs = new List<string>();



        public void Initialize()
        {
            AsyncTwitchEvents.PollCompleted += onPollComplete;
            AsyncTwitchEvents.ChannelPointRedeemed += onChannelPointsRedeemed;
        }

        // can create polls and shit but won't listen to them by default
        public void ListenForEvents()
        {
            AsyncTwitchEvents.Initialize();
        }

        private void onChannelPointsRedeemed(string rewardID)
        {
            throw new NotImplementedException();
        }

        private void onPollComplete(string pollID)
        {
            throw new NotImplementedException();
        }

        public void cleanup()
        {
            // cleanup rewards, should be done on destroy of mod
            foreach(string item in RewardIDs)
            {
                DeleteReward(item);
            }
        }

        // use wrapper to create a poll
        public void CreatePoll(string title, List<string> choices)
        {
            // call Polls.CreatePoll
            string a = Polls.CreatePoll(Authorization, ClientID, ChannelID, title, choices, pollDuration, pollChannelPointVoting, pollCostPerVote);
            
            if(a != string.Empty)
            {
                //add poll id to list
                CurrentPollIDs.Add(a);
            }
            // else polls.createpoll == string.empty // poll creation failed, check token/auth, if issue persists, contact dev
            Plugin.mls.LogInfo("Poll creation failed, please check your auth/token/IDs. If issue persists, contact iminxx on discord.");
        }

        public void CreateReward(string title, int cost, bool genID)
        {
            string result = CustomRewards.createReward(ChannelID, ClientID, Authorization, title, cost, genID);

            if(result != string.Empty)
            {
                // success
                RewardIDs.Add(result);
            }
            else
            {
                Plugin.mls.LogError("Failed to create reward, please review log for potential solution or contact the dev if instructed");
            }
        }

        public void DeleteReward(string id)
        {
            bool result = CustomRewards.deleteReward(id, ChannelID, ClientID, Authorization);
            if (result)
            {
                RewardIDs.Remove(id);
            }
            else
            {
                Plugin.mls.LogError("Fatal Error: Failed to delete reward, review console for instructions to fix");
            }
        }
    }
}
