// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Wait.cs" company="yoshikazu yananose">
//   (c) 2016 machi no omochaya-san.
// </copyright>
// <summary>
//   The wait.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Omochaya
{
    using UnityEngine;

    public class Wait
    {
        // fields
        private float second;

        // constructor
        public Wait(float second)
        {
            this.second = second;
        }

        // methods
        public bool Update()
        {
            this.second -= Time.deltaTime;
            return 0f < this.second;
        }
    }
}