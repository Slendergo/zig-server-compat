﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.realm;

namespace wServer.logic.behaviors
{
    class Prioritize : Behavior
    {
        //State storage: none

        CycleBehavior[] children;
        public Prioritize(params CycleBehavior[] children)
        {
            this.children = children;
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            state = -1;
            foreach (var i in children)
                i.OnStateEntry(host, time);
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            int index;
            if (state == null) index = -1;
            else index = (int)state;

            if (index < 0)    //select
            {
                index = 0;
                for (int i = 0; i < children.Length; i++)
                {
                    children[i].Tick(host, time);
                    if (children[i].Status == CycleStatus.InProgress)
                    {
                        index = i;
                        break;
                    }
                }
            }
            else                //run a cycle
            {
                children[index].Tick(host, time);
                if (children[index].Status == CycleStatus.Completed ||
                    children[index].Status == CycleStatus.NotStarted)
                    index = -1;
            }

            state = index;
        }
    }
}
