using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using FinePrint.Contracts.Parameters;
using KSP;
using KSPAchievements;
using MyAsteroidScanner.Contracts.Parameters;

namespace MyAsteroidScanner.Contracts
{
	public class AsteroidRedirectContract: Contract
	{
		string AsteroidVesselID = "";
		//string AsteroidPartID = "";
		string Description1 = "";
		string Description2 = "";
		string AsteroidName = "";
		bool EmptyContract = false;

		public AsteroidRedirectContract()
		{
			AsteroidVesselID = "";
			EmptyContract = true;
			this.prestige = Contract.ContractPrestige.Trivial;
		}

		public string getAsteroidID ()
		{
			return AsteroidVesselID;
		}

		public bool getAvailable()
		{
			return EmptyContract;
		}

		public bool SetAsteroidID (Vessel v)
		{
			Debug.Log ("Testing new contract ");
			AsteroidVesselID = v.id.ToString();

			if (AsteroidVesselID == "") 
				return false;
			Debug.Log ("Generating new contract for AsteroidVesselID " + AsteroidVesselID);

			//AsteroidPartID  = v.parts.Find(s => s.name == "PotatoRoid").flightID.ToString ();
			//AsteroidPartID = v.id.ToString();



			Description2 = "An incomming astroid has been detected! We need you to divert:";
			AsteroidName = v.vesselName;

			CelestialBody targetBody = FlightGlobals .Bodies [1]; //Kerbin.
			base.SetDeadlineYears (1f, targetBody);


			Debug.Log ("Set Asteroid as detected " );
			GetParameter<AsteroidDectectedParameters> ( MissionSeed.ToString() + "Dectected").SetAsteroidName(AsteroidName);
			GetParameter<AsteroidRedirectAltitudeParameters> ( MissionSeed.ToString() + "Altitude").SetAsteroidName(AsteroidName);
			GetParameter<AsteroidRedirectDockingParameters> (  MissionSeed.ToString() + "Docking").SetAsteroidValues(AsteroidName,AsteroidVesselID);

			EmptyContract = false;

			return true;
		}
			

		protected override bool Generate ()
		{
			Description1 ="We have reason to believe an asteroid is Heading for Kerbin. We want you to divert the next detected asteroid by putting it in a stable orbit around Kerbin.";
			Description2 = "This contract will update as soon as new asteroid is detected.";
			CelestialBody targetBody = FlightGlobals .Bodies [1]; //Kerbin.
			base.SetExpiry ();
			base.SetScience (2.25f, targetBody);
			base.SetDeadlineYears (1f, targetBody);
			base.SetReputation (150f, 60f, targetBody);
			base.SetFunds(100f, 50000f, 75000f, targetBody);

			AddParameter (new AsteroidDectectedParameters (), MissionSeed.ToString() + "Dectected");
			AddParameter (new AsteroidRedirectAltitudeParameters (), MissionSeed.ToString() + "Altitude");
			AddParameter (new AsteroidRedirectDockingParameters (), MissionSeed.ToString() + "Docking");
			this.AddParameter(new StabilityParameter(10));

			return true;
		}


		public override bool CanBeCancelled ()
		{
			return true;
		}
		public override bool CanBeDeclined ()
		{
			return true;
		}
		protected override string GetHashString ()
		{
			return AsteroidVesselID;
		}
		protected override string GetTitle ()
		{
			return "Divert Asteroid " + AsteroidVesselID;
		}
		protected override string GetDescription ()
		{
			//those 3 strings appear to do nothing
			return Description1 + Description2 + AsteroidName;
				//TextGen.GenerateBackStories (Agent.Name, Agent.GetMindsetString (), "Divert", "Asteroid", "Save Kerbin", new System.Random ().Next());
		}
		protected override string GetSynopsys ()
		{
			return "Divert Asteroid " + AsteroidName;
		}
		protected override string MessageCompleted ()
		{
			return "You have succesfully diverted Asteroid " + AsteroidName;
		}

		protected override void OnLoad (ConfigNode node)
		{
			AsteroidVesselID = node.GetValue ("AsteroidVesselID");
			AsteroidName = node.GetValue ("AsteroidName");
			Description1 = node.GetValue ("Description1");
			Description2 = node.GetValue ("Description2");
			EmptyContract = node.GetValue("EmptyContract") ==  "True";
		}
		protected override void OnSave (ConfigNode node)
		{
			node.AddValue ("AsteroidVesselID", AsteroidVesselID.ToString());
			node.AddValue ("AsteroidName", AsteroidName.ToString());
			node.AddValue ("Description1", Description1.ToString());
			node.AddValue ("Description2", Description2.ToString());
			node.AddValue ("EmptyContract", EmptyContract.ToString());
		}

		public override bool MeetRequirements ()
		{
			//Show up in Mission Control
			//@@@TODO: Check for Tracking Station upgrade For Asteroid detection


			return true;
		}



	}
}
	