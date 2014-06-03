﻿// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using MSTS;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;
using ORTS;
using LibAE;
using LibAE.Common;

namespace LibAE.Formats
{
    public class StationPaths
    {
        // Fields
        private List<StationPath> paths = new List<StationPath>();
        public double MaxPassingYard;
        public double ShortPassingYard;

        // Methods
        public StationPaths()
        {
            MaxPassingYard = 0;
            ShortPassingYard = double.PositiveInfinity;
        }

        public void Clear()
        {
            if (paths == null)
            {
                paths = new List<StationPath>();
            }
            foreach (StationPath path in paths)
            {
                path.Clear();
            }
            paths.Clear();
            MaxPassingYard = 0;
            ShortPassingYard = double.PositiveInfinity;
        }

        public void explore(AETraveller myTravel, List<TrackSegment> listConnector, MSTSItems aeItems, StationItem parent)
        {
            List<AEJunctionItem> insideJunction = new List<AEJunctionItem>();
            Stopwatch stopWatch = new Stopwatch();
            TimeSpan ts;
            string elapsedTime;
            stopWatch.Start();
            TrackNode currentNode = myTravel.GetCurrentNode();
            int pathChecked = 0;
            int trackNodeIndex = myTravel.TrackNodeIndex;
            int lastCommonTrack = trackNodeIndex;
            int trackVectorSectionIndex = myTravel.TrackVectorSectionIndex;
            TrVectorSection currentSection = myTravel.GetCurrentSection();
            GlobalItem startNode = aeItems.getTrackSegment(currentNode, trackVectorSectionIndex);
            //paths.Add(new StationPath(startNode, myTravel));
            paths.Add(new StationPath(myTravel));
            paths[0].LastCommonTrack = trackNodeIndex;
            while ((pathChecked < paths.Count && !paths[pathChecked].complete) && paths.Count < 100)
            {
                TrackNode node2 = paths[pathChecked].explore(aeItems, listConnector, trackNodeIndex, parent);
                ts = stopWatch.Elapsed;

                // Format and display the TimeSpan value. 
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Console.WriteLine("RunTime " + elapsedTime);

                if (node2.TrJunctionNode != null)
                {
                    AEJunctionItem junction = (AEJunctionItem)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count-1];
                    if (!insideJunction.Contains(junction))
                    {
                        insideJunction.Add(junction);
                    }
                    if (node2.TrPins[0].Link == lastCommonTrack)
                    {
                        paths[pathChecked].jctnIdx = paths[pathChecked].ComponentItem.Count - 1;
                        paths[pathChecked].LastCommonTrack = lastCommonTrack;
                        paths.Add(new StationPath(paths[pathChecked]));
                        paths[pathChecked].directionJunction = 0;
                        paths[pathChecked].switchJnct(0);
                    }
                    else
                        paths[pathChecked].NextNode();
                }
                else if (node2.TrEndNode)
                {
                    AEBufferItem buffer = (AEBufferItem)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count-1];
                    if (!buffer.Configured || buffer.DirBuffer == AllowedDir.OUT)
                    {
                        //AEJunctionItem junction = (AEJunctionItem)paths[pathChecked].ComponentItem[paths[pathChecked].jctnIdx];
                        paths.RemoveAt(pathChecked);
                    }
                    else
                    {
                        pathChecked++;
                    }
                    if (pathChecked < paths.Count)
                    {
                        paths[pathChecked].switchJnct(paths[pathChecked].directionJunction);
                        if (paths[pathChecked].ComponentItem.Count > 1)
                            lastCommonTrack = (int)paths[pathChecked].LastCommonTrack;
                        else
                            lastCommonTrack = trackNodeIndex;

                    }
                }
                else 
                {
                    int lastIndex = (int)node2.Index;
                    //lastCommonTrack = (int)node2.Index;
                    if (paths[pathChecked].complete)
                    {
                        TrackSegment segment = (TrackSegment)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count - 1];
                        
                        if (segment.HasConnector == null ||
                            (segment.HasConnector != null && 
                            (segment.HasConnector.dirConnector == AllowedDir.IN || !segment.HasConnector.isConfigured())))
                        {
                            paths.RemoveAt(pathChecked);
                        }
                        else
                        {
                            pathChecked++;
                        }
                        //pathChecked++;
                        if (pathChecked < paths.Count)
                        {
                            lastIndex = (int)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count - 2].associateNode.Index;
                            paths[pathChecked].switchJnct(paths[pathChecked].directionJunction);
                        }
                    }
                    if (pathChecked < paths.Count)
                    {
                        if (paths[pathChecked].ComponentItem.Count > 1)
                        {
                            lastCommonTrack = lastIndex;
                            //lastCommonTrack = (int)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count - 2].associateNode.Index;
                        }
                        else
                            lastCommonTrack = trackNodeIndex;
                    }
                }
            }
            stopWatch.Stop();
            ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value. 
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);

            pathChecked = 1;
            foreach (StationPath path in paths)
            {
                if (path.PassingYard > MaxPassingYard)
                    MaxPassingYard = path.PassingYard;
                if (path.PassingYard < ShortPassingYard)
                    ShortPassingYard = path.PassingYard;
                foreach (string elapse in path.elapse)
                {
                    Console.WriteLine("RunTime " + elapse);
                }
            }
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            foreach (StationPath path in paths)
            {
                path.highlightTrackFromArea(aeItems);
            }
        }

        public List<StationPath> getPaths()
        {
            return paths;
        }
    }

    public class StationPath
    {
        // Fields
        public List<GlobalItem> ComponentItem { get; protected set; }
        public List<SideItem> SidesItem { get; protected set; }
        public List<string> elapse { get; set; }
        // Properties
        public bool complete { get; protected set; }
        public double Siding { get; protected set; }
        public double Platform { get; protected set; }
        public double PassingYard { get; protected set; }
        public int jctnIdx { get; set; }
        public int LastCommonTrack { get; set; }
        public short directionJunction { get; set; }
        public int NbrPlatform { get; protected set; }
        public int NbrSiding { get; protected set; }
        public bool MainPath { get; protected set; }
        public AETraveller traveller { get; private set; }
        protected StationPaths parent;

        // Methods
        public StationPath()
        {
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            elapse = new List<string>();
            complete = false;
            jctnIdx = -1;
            traveller = null;
            Siding = 0;
            Platform = 0;
            PassingYard = 0;
            NbrPlatform = 0;
            NbrSiding = 0;
            MainPath = true;
            LastCommonTrack = 0;
            directionJunction = 0;
        }

        public StationPath(StationPath original)
        {
            traveller = new AETraveller(original.traveller);
            MainPath = false;
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            elapse = new List<string>();
            if (original.ComponentItem.Count > 0)
            {
                foreach (GlobalItem componentItem in original.ComponentItem)
                {
                    ComponentItem.Add(componentItem);
                }
                foreach (SideItem sideItem in original.SidesItem)
                {
                    SidesItem.Add(sideItem);
                }
                complete = false;
                if (original.ComponentItem[original.ComponentItem.Count - 1].GetType() == typeof(AEJunctionItem))
                {
                    jctnIdx = original.ComponentItem.Count - 1;
                }
                else
                {
                    jctnIdx = -1;
                }
                NbrPlatform = original.NbrPlatform;
                NbrSiding = original.NbrSiding;
                Siding = original.Siding;
                Platform = original.Platform;
                PassingYard = original.PassingYard;
                LastCommonTrack = original.LastCommonTrack;
                directionJunction = 1;
            }
        }

        public StationPath(GlobalItem startNode, AETraveller travel)
        {
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            elapse = new List<string>();
            ComponentItem.Add(startNode);
            complete = false;
            jctnIdx = -1;
            traveller = new AETraveller(travel);
            Siding = 0;
            Platform = 0;
            PassingYard = 0;
            NbrPlatform = 0;
            NbrSiding = 0;
            LastCommonTrack = 0;
            directionJunction = 0;
        }

        public StationPath(AETraveller travel)
        {
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            elapse = new List<string>();
            complete = false;
            jctnIdx = -1;
            traveller = travel;
            Siding = 0;
            Platform = 0;
            PassingYard = 0;
            NbrPlatform = 0;
            NbrSiding = 0;
            LastCommonTrack = 0;
        }

        public void Clear()
        {
            if (ComponentItem == null)
            {
                ComponentItem = new List<GlobalItem>();
            }
            ComponentItem.Clear();
            SidesItem.Clear();
            elapse.Clear();
        }

        public TrackNode explore(MSTSItems aeItems, List<TrackSegment> listConnector, int entryNode, StationItem parent)
        {
#if false
            Stopwatch stopWatch = new Stopwatch();
            TimeSpan ts;
            string elapsedTime;
            
#endif
            TrackNode currentNode = traveller.GetCurrentNode();
            if ((currentNode.TrJunctionNode == null) && !currentNode.TrEndNode)
            {
                do
                {
                    int sectionIdx = traveller.TrackVectorSectionIndex;
                    TrackSegment item = (TrackSegment)aeItems.getTrackSegment(currentNode, sectionIdx);
                    foreach (TrackSegment conSeg in listConnector)
                    {
                        if (conSeg.associateNodeIdx == entryNode)
                            continue;
                        //  Il faut tester que l'on change bien d'index de node pour quitter  mais pas pour le premier et aussi l'idx de la section
                        if (currentNode.Index == conSeg.associateNodeIdx && sectionIdx == conSeg.associateSectionIdx)
                        {
                            setComplete();
                            break;
                        }
                    }

                    item.inStationArea = true;
                    ComponentItem.Add(item);
                    ((TrackSegment)item).InStation(parent);

                    foreach (var trItem in item.sidings)
                    {
                        SidesItem.Add(trItem);
                        if (trItem.typeSiding == (int)TypeSiding.SIDING_START)
                        {
                            NbrSiding++;
                            if (trItem.sizeSiding > Siding)
                                Siding = trItem.sizeSiding;
                        }
                        else if (trItem.typeSiding == (int)TypeSiding.PLATFORM_START)
                        {
                            NbrPlatform++;
                            if (trItem.sizeSiding > Platform)
                                Platform = trItem.sizeSiding;
                        }
                    }
                    //yard += sideItem.lengthSegment;
#if false
                    ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value. 
                    elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    elapse.Add(elapsedTime);
                    
#endif
                } while (traveller.NextVectorSection() && !complete) ;
                if (currentNode.Index != entryNode && !complete && traveller.TrackNodeLength > PassingYard)
                    PassingYard = traveller.TrackNodeLength;
                traveller.NextTrackNode();
            }
            else
            {
                GlobalItem item = aeItems.getTrackSegment(currentNode, 0);
                item.inStationArea = true;
                ComponentItem.Add(item);
            }
            if (currentNode.TrEndNode)
            {
                complete = true;
            }
            return currentNode;
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            foreach (GlobalItem item in ComponentItem)
            {
                if (item.GetType() == typeof(TrackSegment))
                {
                    ((TrackSegment)item).setAreaSnaps(aeItems);
                }
            }
        }

        public void setComplete()
        {
            complete = true;
        }

        public AETraveller switchJnct(short direction)
        {
            TrackNode junction = traveller.GetCurrentNode();
            if (junction.TrJunctionNode != null)
            {
                junction.TrJunctionNode.SelectedRoute = direction;
            }
            traveller.NextTrackNode();
            return traveller;
        }

        public void NextNode()
        {
            traveller.NextTrackNode();
        }
    }
}