using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using KSP;
using Contracts;
//using KSPPluginFramework;

using KACMyAsteroidScanner_KACWrapper;
//using KACWrapper;

namespace MyAsteroidScanner
{
	[KSPScenario(
		ScenarioCreationOptions.AddToAllGames,
//		GameScenes.SPACECENTER,
		GameScenes.FLIGHT,
		GameScenes.TRACKSTATION
	)]
	public class AsteroidScannerMod : ScenarioModule
	{
		[KSPField(isPersistant = true)]
		//private static bool AddContract = false;
		private static bool AutoTrack = true;
		//private static bool AutoAcceptContract = false;
		private static bool AutoAddKACALarm = true;
		//private static bool PopPupInfo = true;
		//private static bool PauseGame = true;
		//private static bool HeadingForKSC = false; //Hard to do?
		//private static bool RaiseSpawnLimit = false ;
		private static int CheckAltitude = 100000;

		/// <summary>
		/// Stuff
		/// </summary>
		void Start ()
		{
			Debug.Log("Running Start () " );
			//GameEvents.onVesselCreate.Add (CheckNewVessel);
			GameEvents.onNewVesselCreated.Add(CheckNewVessel);
			//GameEvents.onTimeWarpRateChanged.Add(FindKerbinKillerAsteroids);

			//@@@TODO: Load info from Config File
			AutoAddKACALarm = true;
			AutoTrack = true;
			CheckAltitude = 100000;

			KACWrapper.InitKACWrapper();
			if (KACWrapper.APIReady)
			{
				//All good to go
				Debug.Log("KACWrapper Ready");
			}
		}


		/// <summary>
		/// Stuff
		/// </summary>
		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			Debug.Log("Running OnLoad");
			FindKerbinKillerAsteroids ();

			//Debug functions
			//FlightGlobals.Vessels.ForEach (Print_Vessel_info);

		}

		/// <summary>
		/// Stuff
		/// </summary>
		public override void  OnAwake()
		{
			base.OnAwake();
			Debug.Log("Running OnAwake");

			//Debug functions
			//FlightGlobals.Vessels.ForEach (Print_Vessel_info);
			if (FlightGlobals.ready) {
				FindKerbinKillerAsteroids ();
			} else {
				Debug.Log("FlightGlobal Not Ready. Could not check for Killer Asterdois" );
			}

		}
		/// <summary>
		/// Stuff
		/// </summary>
		private static bool CheckOrbits(Orbit o){
			if (o == null) {
				Debug.Log("Orbit is null");
				return false;
			}
			if (!o.activePatch) {
				Debug.Log("Ran out of orbits to check. (last one unactive)");
				return false;
			}
			Debug.Log("Cheking Orbit ");

			if (o.referenceBody.bodyName == FlightGlobals.Bodies [1].bodyName) {
				if (o.PeA < CheckAltitude) {
					Debug.Log("We found an Orbit ");
					return true;
				}
			}

			Debug.Log("Is not intercepting. Next one.");

			if(o.nextPatch != null){
				return CheckOrbits (o.nextPatch);
			}

			Debug.Log("Ran out of orbits to check ");

			return false;
		}

		/// <summary>
		/// Stuff
		/// </summary>
		private static void AsteroidIsDeadly(Vessel v)
		{
			Debug.Log("Running AsteroidIsDeadly(), AutoTrack:"  + AutoTrack);
			//@@@TODO

			//Add to List Of Asteroid to check

			//Track Asteroid if auto-track 

			if (AutoTrack)
				TrackAsteroid(v);


			//Add KAC at SOI Time
			if(AutoAddKACALarm)
				MakeNewAlarm( v);

			//Add Contract if possible

			//
		}

		/// <summary>
		/// Stuff
		/// </summary>
		private static void MakeNewAlarm(Vessel v){

		if (KACWrapper.APIReady) {
			//Check if Alarm already exist
				Predicate<KACWrapper.KACAPI.KACAlarm> VesselFinder = (KACWrapper.KACAPI.KACAlarm a) => {return a.VesselID == v.id.ToString();};
				KACWrapper.KACAPI.KACAlarm OldAlarm = KACWrapper.KAC.Alarms.Find(VesselFinder);
				//String aID = OldAlarm.ID;
				//Debug.Log ("String aID:" + aID);

				//If not
				if (OldAlarm == null) {
					Debug.Log ("Adding New Alarm"); 
					String aID = KACWrapper.KAC.CreateAlarm (KACWrapper.KACAPI.AlarmTypeEnum.SOIChange, v.RevealName () + " Heading for Kerbin", v.orbit.UTsoi);
					if (aID != "") {
						//if the alarm was made get the object so we can update it
						KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.First (z => z.ID == aID);

						//Now update some of the other properties
						a.Notes = "An Asteroid is on a collision Course for Kerbin";
						a.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
						a.VesselID = v.id.ToString ();
					}

				} else {
					Debug.Log ("Alarm Already exist:" + OldAlarm.ID);
				}


			}

		}

		/// <summary>
		/// Stuff
		/// </summary>
		public void CheckNewVessel(Vessel v){
			Debug.Log("Running CheckNewVessel()" );
			if (HighLogic.LoadedScene != GameScenes.TRACKSTATION && HighLogic.LoadedScene != GameScenes.FLIGHT )
				return;
			
			if(CheckVessel (v)){
				
				AsteroidIsDeadly(v);
			}
		}

		/// <summary>
		/// Stuff
		/// </summary>
		private static bool CheckVessel(Vessel v){
			bool answer = false;
			Debug.Log("Cheking Intersection for Vessel " + v.name );
			bool DontdetachConics = v.PatchedConicsAttached; 


			//Debug Output
			string Output = "";

			Output = "Name:" + v.name + "; Of type:" +  v.vesselType +  "; doing: " + v.RevealSituationString () +
				"; Unobserved for: " + v.DiscoveryInfo.unobservedLifetime + "sec; Orbiting:" + v.orbit.referenceBody.name ;

			Debug.Log(Output);


			if (!v.PatchedConicsAttached) {
				Debug.Log("No PatchedConicSolver. Creating a New One. ");
				try {
					//v.patchedConicSolver = new PatchedConicSolver ();
					v.AttachPatchedConicsSolver();
					v.patchedConicSolver.IncreasePatchLimit ();
					v.patchedConicSolver.Update ();
				} 
				catch(Exception ex)
				{
					Debug.Log("Could not attach patchedConicSolver. Exception:" + ex);
					//DontdetachConics = true;
					//v.DetachPatchedConicsSolver ();
				}
			}

			answer = CheckOrbits (v.orbit);
				

			if (!DontdetachConics && v.PatchedConicsAttached && v.patchedConicSolver != null) {
				v.DetachPatchedConicsSolver ();
			}
			return answer;
		}

		/// <summary>
		/// Stuff
		/// </summary>
		private static void FindKerbinKillerAsteroids (){


			Debug.Log("Running FindKerbinKillerAsteroids \n");

			foreach(Vessel v in FlightGlobals.Vessels){
				if (v.vesselType == VesselType.SpaceObject) {
					if (CheckVessel (v)) {
						Debug.Log(v.RevealName () + "is Heading for Kerbin.");
						AsteroidIsDeadly(v);

					}
				

				}
			}
			//Pop-up question/Information to the USer.?


		}

		/// <summary>
		/// Stuff
		/// </summary>
		public static void TrackAsteroid(Vessel v)
		{
			Debug.Log ("DiscoveryLevels " + v.DiscoveryInfo.Level );
			if (v.DiscoveryInfo.Level == DiscoveryLevels.None || v.DiscoveryInfo.Level == DiscoveryLevels.Presence ) {
				Debug.Log ("Set tracking to " + v.RevealName ());
				v.DiscoveryInfo.SetLastObservedTime (Planetarium.GetUniversalTime ());
				v.DiscoveryInfo.SetLevel (DiscoveryLevels.Unowned);

			}
		}


		//////////////////////////////
		/// Debug functions
		/// ///////////////////////////	

		/// <summary>
		/// 
		/// </summary>
		/// <param name="o">O.</param>
		private static void Print_Vessel_info(Vessel v)
		{
			Debug.Log("Vessel Info \n");
			bool AttachedConicsAtStart = v.PatchedConicsAttached; 

			string Output = "";

			Output = "Name:" + v.name + "; Of type:" +  v.vesselType +  "; doing: " + v.RevealSituationString () +
				"; Unobserved for: " + v.DiscoveryInfo.unobservedLifetime + "sec; Orbiting:" + v.orbit.referenceBody.name ;

			Debug.Log(Output);






			//Dump All info
			Output = "Vessel Dump \n";

			//Variables
			//Output += "\n acceleration:" + v.acceleration;
			Output += "\n altitude:" + v.altitude;
			//Output += "\n angularMomentum:" + v.angularMomentum;
			//Output += "\n angularVelocity:" + v.angularVelocity;
			//Output += "\n atmDensity:" + v.atmDensity;
			//Output += "\n CoM:" + v.CoM;
			Output += "\n ctrlState:" + v.ctrlState;
			//Output += "\n currentStage:" + v.currentStage;
			//Output += "\n geeForce:" + v.geeForce;
			//Output += "\n geeForce_immediate:" + v.geeForce_immediate;
			//Output += "\n heightFromSurface:" + v.heightFromSurface;
			//Output += "\n heightFromTerrain:" + v.heightFromTerrain;
			//Output += "\n horizontalSrfSpeed:" + v.horizontalSrfSpeed;
			Output += "\n id:" + v.id;
			//Output += "\n isEVA:" + v.isEVA;
			//Output += "\n Landed:" + v.Landed;
			//Output += "\n LandedAt:" + v.landedAt;
			Output += "\n Latitude:" + v.latitude;
			Output += "\n LaunchTime:" + v.launchTime;
			Output += "\n loaded:" + v.loaded;
			//Output += "\n LocalCoM:" + v.localCoM;
			//Output += "\n missionTime:" + v.missionTime;
			//Output += "\n MOI:" + v.MOI;
			//Output += "\n obt_speed:" + v.obt_speed;
			//Output += "\n obt_velocity:" + v.obt_velocity;
//			Output += "\n A:" + v.OnFlyByWire;
			Output += "\n orbitDriver:" + v.orbitDriver;
			Output += "\n orbitTargeter:" + v.orbitTargeter;
			Output += "\n packed:" + v.packed;
			//Output += "\n parts:" + v.parts;
			Output += "\n patchedConicRenderer:" + v.patchedConicRenderer;
			Output += "\n patchedConicSolver:" + v.patchedConicSolver;
			//Output += "\n perturbation:" + v.perturbation;
			//Output += "\n pqsAltitude:" + v.pqsAltitude;
			Output += "\n protoVessel:" + v.protoVessel;
			//Output += "\n rb_velocity:" + v.rb_velocity;
			//Output += "\n referenceTransformId:" + v.referenceTransformId;
			//Output += "\n rootPart:" + v.rootPart;
			Output += "\n situation:" + v.situation;
			//Output += "\n specificAcceleration:" + v.specificAcceleration;
			//Output += "\n Splashed:" + v.Splashed;
			//Output += "\n srf_velocity:" + v.srf_velocity;
			//Output += "\n srfRelRotation:" + v.srfRelRotation;
			Output += "\n state:" + v.state;
			//Output += "\n staticPressurekPa:" + v.staticPressurekPa;
			//Output += "\n terrainAltitude:" + v.terrainAltitude;
			//Output += "\n terrainNormal:" + v.terrainNormal;
			//Output += "\n upAxis:" + v.upAxis;
			Output += "\n verticalSpeed:" + v.verticalSpeed;
			Output += "\n vesselName:" + v.vesselName;
			Output += "\n vesselType:" + v.vesselType;

			//Properties (get/set)
			//Output += "\n ActionGroups:" + v.ActionGroups;
			Output += "\n DiscoveryInfo:" + v.DiscoveryInfo.ToString(); //@@@TODO: add Function

			Output += "\n HoldPhysics:" + v.HoldPhysics;
			Output += "\n isActiveVessel:" + v.isActiveVessel;
			Output += "\n isCommandable:" + v.isCommandable;
			Output += "\n IsControllable:" + v.IsControllable;
			Output += "\n isPersistent:" + v.isPersistent;
			Output += "\n LandedOrSplashed:" + v.LandedOrSplashed;
			Output += "\n orbit:" + v.orbit;

			//functions
			Output += "\n PatchedConicsAttached:" + v.PatchedConicsAttached;

//			Debug.Log(v.GetComponents<Component> ());


			//tests
			if(v.PatchedConicsAttached){
				///@@@TODO: Add another funtion for PatchConic Solver Dump
				Output += "\n maxTotalPatches:" + v.patchedConicSolver.maxTotalPatches;
			}
			Debug.Log(Output);
			Print_Discovery_info (v.DiscoveryInfo);
			Print_Orbit_info (v.orbit);


			if (!v.PatchedConicsAttached) {
				Debug.Log("No PatchedConicSolver. Creating a New One. ");
				//v.patchedConicSolver = new PatchedConicSolver ();
				try {
					v.AttachPatchedConicsSolver ();
					v.patchedConicSolver.Update ();
					v.patchedConicSolver.IncreasePatchLimit ();
					v.patchedConicSolver.Update ();
					v.patchedConicSolver.IncreasePatchLimit ();
					v.patchedConicSolver.Update ();
					v.patchedConicSolver.IncreasePatchLimit ();
					v.patchedConicSolver.Update ();
					Print_Orbit_info (v.orbit);
				} 
				catch(Exception ex)
				{
					Debug.Log("Could not attach patchedConicSolver. Exception:" + ex);
					AttachedConicsAtStart = true;
					//v.DetachPatchedConicsSolver ();
				}
			}

			if (!AttachedConicsAtStart) {
				v.DetachPatchedConicsSolver ();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="o">O.</param>
		private static void Print_Orbit_info(Orbit o)
		{
			if (o == null) {
				Debug.Log("Orbit is null");
				return;
			}
			string Output = "";

			Output +=  "Orbit Info \n";
			Output += "\n activePatch:" + o.activePatch ;
			Output += "\n altitude:"+ o.altitude ;
			//Output += "\n an:"+ o.an ;
			//Output += "\n ApA:"+ o.ApA ;
			//Output += "\n ApR:"+ o.ApR ;
			Output += "\n argumentOfPeriapsis:"+ o.argumentOfPeriapsis ;
			//Output += "\n ClAppr:"+ o.ClAppr ;
			//Output += "\n ClEctr1:"+ o.ClEctr1 ;
			//Output += "\n ClEctr2:"+ o.ClEctr2 ;
			Output += "\n closestEncounterBody:"+ o.closestEncounterBody ;
			Output += "\n closestEncounterLevel:"+ o.closestEncounterLevel ;
			Output += "\n closestEncounterPatch:"+ o.closestEncounterPatch ;
			Output += "\n closestTgtApprUT:"+ o.closestTgtApprUT ;
			//Output += "\n CrAppr:"+ o.CrAppr ;
			//Output += "\n debugAN:"+ o.debugAN ;
			//Output += "\n debugEccVec:"+ o.debugEccVec ;
			//Output += "\n debugH:"+ o.debugH ;
			//Output += "\n debugPos:"+ o.debugPos ;
			//Output += "\n debugVel:"+ o.debugVel ;
			//Output += "\n debug_returnFullEllipseTrajectory:"+ o.debug_returnFullEllipseTrajectory ;
			//Output += "\n E:"+ o.E ;
			//Output += "\n eccentricAnomaly:"+ o.eccentricAnomaly ;
			//Output += "\n eccentricity:"+ o.eccentricity ;
			//Output += "\n eccVec:"+ o.eccVec ;
			//Output += "\n EndUT:"+ o.EndUT ;
			Output += "\n epoch:"+ o.epoch ;
			//Output += "\n FEVp:"+ o.FEVp;
			//Output += "\n FEVs:"+ o.FEVs ;
			//Output += "\n fromE:"+ o.fromE ;
			//Output += "\n fromV:"+ o.fromV ;
			//Output += "\n h:"+ o.h ;
			//Output += "\n inclination:"+ o.inclination ;
			//Output += "\n LAN:"+ o.LAN ;
			//Output += "\n mag:"+ o.mag ;
			//Output += "\n meanAnomaly:"+ o.meanAnomaly ;
			//Output += "\n meanAnomalyAtEpoch:"+ o.meanAnomalyAtEpoch ;
			Output += "\n nearestTT:"+ o.nearestTT ;
			Output += "\n nextPatch:"+ o.nextPatch ;
			Output += "\n nextTT:"+ o.nextTT ;
			//Output += "\n ObT:"+ o.ObT ;
			Output += "\n ObTAtEpoch :"+ o.ObTAtEpoch ;
			//Output += "\n orbitalEnergy :"+ o.orbitalEnergy ;
			//Output += "\n orbitalSpeed:"+ o.orbitalSpeed ;
			//Output += "\n orbitPercent :"+ o.orbitPercent ;
			Output += "\n patchEndTransition :"+ o.patchEndTransition ;
			Output += "\n patchStartTransition :"+ o.patchStartTransition ;
			//Output += "\n period :"+ o.period ;
			//Output += "\n pos :"+ o.pos ;
			Output += "\n previousPatch :"+ o.previousPatch ;
			//Output += "\n radius :"+ o.radius ;
			Output += "\n referenceBody :"+ o.referenceBody ;
			//Output += "\n sampleInterval :"+ o.sampleInterval ;
			Output += "\n secondaryPosAtTransition1 :"+ o.secondaryPosAtTransition1 ;
			Output += "\n secondaryPosAtTransition2 :"+ o.secondaryPosAtTransition2 ;
			//Output += "\n semiLatusRectum :"+ o.semiLatusRectum ;
			//Output += "\n semiMajorAxis :"+ o.semiMajorAxis ;
			//Output += "\n semiMinorAxis :"+ o.semiMinorAxis ;
			//Output += "\n SEVp :"+ o.SEVp ;
			//Output += "\n SEVs :"+ o.SEVs ;
			//Output += "\n StartUT :"+ o.StartUT ;
			Output += "\n timeToAp :"+ o.timeToAp ;
			Output += "\n timeToPe :"+ o.timeToPe ;
			Output += "\n timeToTransition1 :"+ o.timeToTransition1 ;
			Output += "\n timeToTransition2 :"+ o.timeToTransition2 ;
			//Output += "\n toE :"+ o.toE ;
			//Output += "\n toV :"+ o.toV ;
			//Output += "\n trueAnomaly :"+ o.trueAnomaly ;
			//Output += "\n UTappr :"+ o.UTappr ;
			//Output += "\n UTsoi :"+ o.UTsoi ;
			//Output += "\n V :"+ o.V ;
			//Output += "\n vel :"+ o.vel ;

			Debug.Log(Output);

			if (!o.activePatch) {
				return;
			}
			try{

				//Functions
				Output = "Orbit Functions: \n";
				Output += "\n PeA :"+ o.PeA ;
				Output += "\n PeR :"+ o.PeR ;
				Output += "\n GetANVector:"+ o.GetANVector() ;
				Output += "\n GetEccVector:"+ o.GetEccVector() ;
				Output += "\n GetFrameVel:"+ o.GetFrameVel() ;
				Output += "\n GetHashCode:"+ o.GetHashCode() ;
				Output += "\n GetOrbitNormal:"+ o.GetOrbitNormal() ;
				Output += "\n GetRelativeVel:"+ o.GetRelativeVel() ;
				Output += "\n GetType:"+ o.GetType() ;
				Output += "\n GetVel:"+ o.GetVel() ;
				Output += "\n GetWorldSpaceVel:"+ o.GetWorldSpaceVel() ;

				//Unused Funtion (Tests?)
				//			Output += "\n :"+ o.DrawOrbit;
				//			Output += "\n :"+ o.Equals ;
				//			Output += "\n :"+ o.GetDTforTrueAnomaly() ;
				//			Output += "\n :"+ o.GetEccentricAnomaly ;
				//			Output += "\n :"+ o.GetFrameVelAtUT ;
				//			Output += "\n :"+ o.GetMeanAnomaly ;
				//			Output += "\n :"+ o.getObTAtMeanAnomaly ;
				//			Output += "\n :"+ o.getObtAtUT ;
				//			Output += "\n :"+ o.getOrbitalSpeedAt ;
				//			Output += "\n :"+ o.getOrbitalSpeedAtDistance ;
				//			Output += "\n :"+ o.getOrbitalSpeedAtPos ;
				//			Output += "\n :"+ o.getOrbitalSpeedAtRelativePos ;
				//			Output += "\n :"+ o.getOrbitalVelocityAtObT ;
				//			Output += "\n :"+ o.getOrbitalVelocityAtUT ;
				//			Output += "\n :"+ o.GetPatchTrajectory ;
				//			Output += "\n :"+ o.getPositionAtT ;
				//			Output += "\n :"+ o.getPositionAtUT ;
				//			Output += "\n :"+ o.getPositionFromEccAnomaly ;
				//			Output += "\n :"+ o.getPositionFromMeanAnomaly ;
				//			Output += "\n :"+ o.getPositionFromTrueAnomaly ;
				//			Output += "\n :"+ o.getRelativePositionAtT ;
				//			Output += "\n :"+ o.getRelativePositionAtUT ;
				//			Output += "\n :"+ o.getRelativePositionFromEccAnomaly ;
				//			Output += "\n :"+ o.getRelativePositionFromMeanAnomaly ;
				//			Output += "\n :"+ o.getRelativePositionFromTrueAnomaly ;
				//			Output += "\n :"+ o.GetRotFrameVel ;
				//			Output += "\n :"+ o.getTrueAnomaly ;
				//			Output += "\n :"+ o.GetTrueAnomalyOfZupVector ;
				//			Output += "\n :"+ o.getTruePositionAtUT ;
				//			Output += "\n :"+ o.GetUTforTrueAnomaly ;
				//			Output += "\n :"+ o.Init() ;
				//			Output += "\n :"+ o.RadiusAtTrueAnomaly ;
				//			Output += "\n :"+ o.solveEccentricAnomaly ;
				//			Output += "\n :"+ o.TrueAnomalyAtRadius ;
				//			Output += "\n :"+ o.TrueAnomalyAtT ;
				//			Output += "\n :"+ o.TrueAnomalyAtUT;
				//			Output += "\n :"+ o.UpdateFromOrbitAtUT;
				//			Output += "\n :"+ o.UpdateFromStateVectors;
				//			Output += "\n :"+ o.UpdateFromUT;
			}
			catch(Exception ex) {
				Debug.Log("Could not orbit functions. Exception:" + ex);
				//no catch exception for now.
			}

			Debug.Log(Output);

			Debug.Log("Patch from o.nextPatch");
			Print_Orbit_info (o.nextPatch);

			Debug.Log("Patch from o.closestEncounterPatch");
			Print_Orbit_info (o.closestEncounterPatch);

			Output = "";
		}

		private static void Print_Discovery_info(DiscoveryInfo I)
		{
			String Output = "Discovery Info \n";
			Output += "\n distance:" + I.distance.Value;
			Output += "\n fadeUT:" + I.fadeUT;
			Output += "\n lastObservedTime:" + I.lastObservedTime;
			Output += "\n Level:" + I.Level;
			Output += "\n mass:" + I.mass.Value;
			Output += "\n name:" + I.name.Value;
			Output += "\n objectSize:" + I.objectSize;
			Output += "\n referenceLifetime:" + I.referenceLifetime;
			Output += "\n signalStrengthLevel:" + I.signalStrengthLevel.Value;
			Output += "\n signalStrengthPercent:" + I.signalStrengthPercent.Value;
			Output += "\n situation:" + I.situation.Value;
			Output += "\n size:" + I.size.Value;
			Output += "\n speed:" + I.speed.Value;
			Output += "\n trackingStatus:" + I.trackingStatus.Value;
			Output += "\n type:" + I.type.Value;
			Output += "\n unobservedLifetime:" + I.unobservedLifetime;

			Output += "\n GetHashCode:" + I.GetHashCode();
			Output += "\n GetType:" + I.GetType();
			//Output += "\n GetSignalLife:" + I.GetSignalLife( );
			//Output += "\n GetSignalStrength:" + I.GetSignalStrength();

			Debug.Log(Output);
		}
	}
}


/////////////////////////////////////////////////////////////////////////////////////
