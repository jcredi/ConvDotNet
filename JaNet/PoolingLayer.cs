﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCL.Net;

namespace JaNet
{
    class PoolingLayer : Layer
    {
        #region Fields

        private string poolingType; // options: "max", "average" (not supported yet), "l2norm" (not supported yet)
        private int poolWidth;
        private int stride;

#if OPENCL_ENABLED
        private Mem poolingTableGPU;
        private Mem switchesGPU;

        private IntPtr[] globalWorkSizePtr;
        private IntPtr[] localWorkSizePtr;
#else
        private int[,] poolingTable;
        private bool[] switches;
#endif

        #endregion


        #region Setup methods

        /// <summary>
        /// Constructor of Pooling layer
        /// </summary>
        /// <param name="PoolingType"></param>
        /// <param name="poolWidth"></param>
        /// <param name="stride"></param>
        public PoolingLayer(string PoolingType, int PoolWidth, int Stride)
        {
            this.type = "Pooling";

            if (PoolWidth != 2 || Stride != 2)
                throw new ArgumentException("Pooling layer: only 2x2 pooling with stride 2 is currently supported.");
            switch (PoolingType)
            {
                case "max":
                {
                    this.poolingType = PoolingType;
                    break;
                }
                default:
                {
                    throw new ArgumentException("Pooling layer: only ''max'' pooling is currently supported.");
                }
            }

            this.poolWidth = PoolWidth;
            this.stride = Stride;
        }

        public override void SetupOutput()
        {
            // Check arguments _______________________________________________________________________________________

            if (inputHeight != inputWidth)
                throw new ArgumentException("PoolingLayer currently only supports spatially square input.");

            if (poolWidth == stride && inputWidth % poolWidth != 0)
                throw new ArgumentException("Cannot apply pooling to input: pooling width and stride do not fit!");

            // Setup output __________________________________________________________________________________________

            this.outputWidth = (inputWidth - poolWidth) / stride + 1;
            this.outputHeight = (inputHeight - poolWidth) / stride + 1;
            this.outputDepth = inputDepth;

            this.nOutputUnits = outputWidth * outputHeight * outputDepth;
            this.outputNeurons = new Neurons(nOutputUnits);

            // Initialize and create auxiliary structures ____________________________________________________________
#if OPENCL_ENABLED

            // Pooling table

            this.poolingTableGPU = (Mem)Cl.CreateBuffer(OpenCLSpace.Context,
                                                        MemFlags.ReadWrite,
                                                        (IntPtr)(sizeof(int) * 4 * outputHeight * outputWidth),
                                                        out OpenCLSpace.ClError);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.CreateBuffer poolingTableGPU");
            OpenCLSpace.WipeBuffer(poolingTableGPU, 4 * outputHeight * outputWidth, typeof(int));

            OpenCLSpace.ClError = Cl.SetKernelArg(OpenCLSpace.CreatePoolingTable, 0, poolingTableGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.CreatePoolingTable, 1, (IntPtr)sizeof(int), stride);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.CreatePoolingTable, 2, (IntPtr)sizeof(int), inputWidth);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.CreatePoolingTable, 3, (IntPtr)sizeof(int), outputWidth);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.SetKernelArg CreatePoolingTable");

            OpenCLSpace.ClError = Cl.EnqueueNDRangeKernel(  OpenCLSpace.Queue,
                                                            OpenCLSpace.CreatePoolingTable,
                                                            1,
                                                            null,
                                                            new IntPtr[] { (IntPtr)(32 * Math.Ceiling((double)(nOutputUnits * inputNeurons.MiniBatchSize) / (double)32)) },
                                                            new IntPtr[] { (IntPtr)32 },
                                                            0,
                                                            null,
                                                            out OpenCLSpace.ClEvent);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.EnqueueNDRangeKernel CreatePoolingTable");

            OpenCLSpace.ClError = Cl.ReleaseEvent(OpenCLSpace.ClEvent);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.ReleaseEvent");


            /* -----------------------------------------------------------------
            int[] table = new int[4 * outputHeight * outputWidth];

            OpenCLSpace.ClError = Cl.EnqueueReadBuffer( OpenCLSpace.Queue,
                                                        poolingTableGPU,
                                                        Bool.True,
                                                        (IntPtr)0,
                                                        (IntPtr)(sizeof(int) * 4 * outputHeight * outputWidth),
                                                        table,
                                                        0,
                                                        null,
                                                        out OpenCLSpace.ClEvent);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.clEnqueueReadBuffer");

            Console.WriteLine("\nTABLE:");

            for (int i = 0; i < outputHeight * outputWidth; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Console.Write("{0} ", table[i * 4 + j]);
                }
                Console.WriteLine();
            }
            Console.ReadKey();

            /* ------------------------------------------------------------------ */ 


            // Switches

            this.switchesGPU = (Mem)Cl.CreateBuffer(OpenCLSpace.Context,
                                                    MemFlags.ReadWrite,
                                                    (IntPtr)(sizeof(bool) * nInputUnits * inputNeurons.MiniBatchSize),
                                                    out OpenCLSpace.ClError);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.CreateBuffer switchesGPU");
            OpenCLSpace.WipeBuffer(switchesGPU, nInputUnits * inputNeurons.MiniBatchSize, typeof(bool));
#else
            //TODO: create poolingTable and switches on cpu

#endif
           
        }


        public override void SetWorkGroups()
        {

            // TODO: method

            
#if OPENCL_ENABLED
            // Work group sizes will be set as follows:
            //      global work size = smallest multiple of OPTIMAL_GROUP_SIZE larger than 
            //                         the total number of processes needed (for efficiency).
            //      local work size = as close as possible to OPTIMAL_GROUP_SIZE (making sure 
            //                        that global worksize is a multiple of this)
            // OPTIMAL_GROUP_SIZE is a small multiple of BASE_GROUP_SIZE, which in turn is a 
            //                    constant multiple of 2, platform-dependent, e.g. 32 (Nvidia 
            //                    WARP) or 64 (AMD WAVEFRONT).

            // Local
            this.localWorkSizePtr = new IntPtr[] { (IntPtr)OpenCLSpace.OPTIMAL_GROUP_SIZE };

            // Global
            int totalWorkItemsNeeded = nOutputUnits * outputNeurons.MiniBatchSize;
            int smallestMultipleOfLocal = (int)(OpenCLSpace.OPTIMAL_GROUP_SIZE * Math.Ceiling((double)(totalWorkItemsNeeded) / (double)OpenCLSpace.OPTIMAL_GROUP_SIZE));
            this.globalWorkSizePtr = new IntPtr[] { (IntPtr)(smallestMultipleOfLocal) };
#endif
             
        }


        #endregion


#region Methods

        public override void FeedForward()
        {
#if OPENCL_ENABLED
            OpenCLSpace.ClError  = Cl.SetKernelArg(OpenCLSpace.PoolingForward, 0, outputNeurons.ActivationsGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 1, inputNeurons.ActivationsGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 2, switchesGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 3, poolingTableGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 4, (IntPtr)sizeof(int), nInputUnits);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 5, (IntPtr)sizeof(int), inputWidth * inputWidth);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 6, (IntPtr)sizeof(int), nOutputUnits);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 7, (IntPtr)sizeof(int), outputWidth * outputWidth);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingForward, 8, (IntPtr)sizeof(int), inputNeurons.MiniBatchSize);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.SetKernelArg PoolingForward");

            OpenCLSpace.ClError = Cl.EnqueueNDRangeKernel(  OpenCLSpace.Queue,
                                                            OpenCLSpace.PoolingForward,
                                                            1,
                                                            null,
                                                            globalWorkSizePtr,
                                                            localWorkSizePtr,
                                                            0,
                                                            null,
                                                            out OpenCLSpace.ClEvent);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.EnqueueNDRangeKernel PoolingForward");

            OpenCLSpace.ClError = Cl.ReleaseEvent(OpenCLSpace.ClEvent);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.ReleaseEvent");

            OpenCLSpace.ClError = Cl.Finish(OpenCLSpace.Queue);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.Finish");
#else
            //TODO: CPU code
#endif

        }

        public override void BackPropagate()
        {
#if OPENCL_ENABLED
            OpenCLSpace.ClError  = Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 0, inputNeurons.DeltaGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 1, outputNeurons.DeltaGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 2, switchesGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 3, poolingTableGPU);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 4, (IntPtr)sizeof(int), nInputUnits);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 5, (IntPtr)sizeof(int), inputWidth * inputWidth);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 6, (IntPtr)sizeof(int), nOutputUnits);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 7, (IntPtr)sizeof(int), outputWidth * outputWidth);
            OpenCLSpace.ClError |= Cl.SetKernelArg(OpenCLSpace.PoolingBackward, 8, (IntPtr)sizeof(int), inputNeurons.MiniBatchSize);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.SetKernelArg PoolingBackward");

            OpenCLSpace.ClError = Cl.EnqueueNDRangeKernel(  OpenCLSpace.Queue,
                                                            OpenCLSpace.PoolingBackward,
                                                            1,
                                                            null,
                                                            globalWorkSizePtr,
                                                            localWorkSizePtr,
                                                            0,
                                                            null,
                                                            out OpenCLSpace.ClEvent);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.EnqueueNDRangeKernel PoolingBackward");

            OpenCLSpace.ClError = Cl.ReleaseEvent(OpenCLSpace.ClEvent);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.ReleaseEvent");

            OpenCLSpace.ClError = Cl.Finish(OpenCLSpace.Queue);
            OpenCLSpace.CheckErr(OpenCLSpace.ClError, "Cl.Finish");
#else
            //TODO: CPU code
#endif
        }

#endregion



    }
}