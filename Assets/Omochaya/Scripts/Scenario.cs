// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Scenario.cs" company="yoshikazu yananose">
//   (c) 2016 machi no omochaya-san.
// </copyright>
// <summary>
//   The scenario.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Omochaya
{
    using System;
    using System.Collections.Generic;

    public class Scenario
    {
        // fields
        private IEnumerator<Func<bool>> current = null;
        private Func<bool> stop = null;

        // constructors
        public Scenario() { }
        public Scenario(IEnumerator<Func<bool>> current)
        {
            this.Set(current);
        }

        // methods
        public void Set(IEnumerator<Func<bool>> current)
        {
            this.current = current;
            this.stop = null;
        }

        // update
        public bool Update()
        {
            if (this.current != null)
            {
                if (this.stop == null || !this.stop())
                {
                    if (this.current.MoveNext())
                    {
                        this.stop = this.current.Current;
                    }
                    else
                    {
                        this.current = null;
                    }
                }
            }

            return this.current != null;
        }
    }
}