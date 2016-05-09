﻿//#define OPENCL_ENABLED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCL.Net;

namespace JaNet
{
    class JaNetProgram
    {

        static void Main(string[] args)
        {

#if OPENCL_ENABLED

            /*****************************************************
             * (0) Setup OpenCL
             ****************************************************/
            Console.WriteLine("\n=========================================");
            Console.WriteLine("    OpenCL setup");
            Console.WriteLine("=========================================\n");

            OpenCLSpace.SetupSpace();
            OpenCLSpace.KernelsPath = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/JaNet/Kernels";
            OpenCLSpace.LoadKernels();  
#endif


            /*****************************************************
             * (1) Instantiate a neural network and add layers
             * 
             * OPTIONS:
             * ConvolutionalLayer(filterSize, numberOfFilters, strideLength, zeroPadding)
             * FullyConnectedLayer(numberOfUnits)
             * MaxPooling(2, 2)
             * ReLU()
             * ELU(alpha)
             * SoftMax()
             ****************************************************/

            Console.WriteLine("\n=========================================");
            Console.WriteLine("    Neural network creation");
            Console.WriteLine("=========================================\n");

            
            NeuralNetwork network = new NeuralNetwork();
            network.Name = "myNetwork1";
             
            network.AddLayer(new InputLayer(1, 32, 32));

            network.AddLayer(new ConvolutionalLayer(5, 32, 1, 0));
            //network.AddLayer(new BatchNormConv());
            network.AddLayer(new ReLU());
            //network.AddLayer(new ELU(1.0f));

            //network.AddLayer(new ConvolutionalLayer(5, 16, 1, 1));
            //network.AddLayer(new BatchNormConv());
            //network.AddLayer(new ReLU());
            
            network.AddLayer(new MaxPooling(2, 2));

            network.AddLayer(new ConvolutionalLayer(3, 64, 1, 0));
            //network.AddLayer(new BatchNormConv());
            network.AddLayer(new ReLU());
            //network.AddLayer(new ELU(1.0f));

            //network.AddLayer(new ConvolutionalLayer(3, 32, 1, 1));
            //network.AddLayer(new BatchNormConv());
            //network.AddLayer(new ReLU());

            network.AddLayer(new MaxPooling(2, 2));
            

            network.AddLayer(new FullyConnectedLayer(128));
            //network.AddLayer(new BatchNormFC());
            network.AddLayer(new ReLU());
            //network.AddLayer(new ELU(1.0f));
            
            network.AddLayer(new FullyConnectedLayer(43));
            network.AddLayer(new SoftMax());


            network.Set("MiniBatchSize", 32);
            network.InitializeParameters("random");
              
            


            /*****************************************************
             * (2) Load data
             ****************************************************/
            Console.WriteLine("\n=========================================");
            Console.WriteLine("    Importing data");
            Console.WriteLine("=========================================\n");

            #region Paths to datasets

            // Original MNIST training set
            //string MNISTtrainingData = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/MNIST/mnistTrainImages.dat";
            //string MNISTtrainingLabels = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/MNIST/mnistTrainLabels.dat";

            // Original MNIST test set
            //string MNISTtestData = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/MNIST/mnistTestImages.dat";
            //string MNISTtestLabels = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/MNIST/mnistTestLabels.dat";

            // Reduced MNIST dataset (1000 data points, 100 per digit)
            //string MNISTreducedData = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/MNIST/mnistImagesSubset.dat";
            //string MNISTreducedLabels = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/MNIST/mnistLabelsSubset.dat";

            // GTSRB training set (grayscale)
            string GTSRBtrainingDataGS = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/08_training_images.dat";
            string GTSRBtrainingLabelsGS = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/08_training_classes.dat";

            // GTSRB validation set (grayscale)
            string GTSRBvalidationDataGS = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/08_validation_images.dat";
            string GTSRBvalidationLabelsGS = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/08_validation_classes.dat";

            // GTSRB test set (grayscale)
            string GTSRBtestDataGS = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/08_test_images.dat";
            string GTSRBtestLabelsGS = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/test_labels_full.dat";

            // GTSRB test set (RGB)
            //string GTSRBtestDataRGB = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/03_test_images.dat";
            //string GTSRBtestLabelsRGB = "C:/Users/jacopo/Dropbox/Chalmers/MSc thesis/GTSRB/Preprocessed/test_labels_full.dat";

            

            #endregion

            // Toy MNIST dataset
            /*
            Console.WriteLine("Importing toy dataset...");
            DataSet toySet = new DataSet(10);
            toySet.ReadData(MNISTreducedData);
            toySet.ReadLabels(MNISTreducedLabels);
            */

            
            Console.WriteLine("Importing training set...");
            DataSet trainingSet = new DataSet(43);
            trainingSet.ReadData(GTSRBtrainingDataGS);
            trainingSet.ReadLabels(GTSRBtrainingLabelsGS);
            

            Console.WriteLine("Importing validation set...");
            DataSet validationSet = new DataSet(43);
            validationSet.ReadData(GTSRBvalidationDataGS);
            validationSet.ReadLabels(GTSRBvalidationLabelsGS);

            
            Console.WriteLine("Importing test set...");
            DataSet testSet = new DataSet(43);
            testSet.ReadData(GTSRBtestDataGS);
            testSet.ReadLabels(GTSRBtestLabelsGS);
            



            /*****************************************************
             * (3) Train network
             ****************\************************************/
            Console.WriteLine("\n=========================================");
            Console.WriteLine("    Network training");
            Console.WriteLine("=========================================\n");


            NetworkTrainer networkTrainer = new NetworkTrainer();

            networkTrainer.LearningRate = 0.005;
            networkTrainer.MomentumMultiplier = 0.9;
            networkTrainer.WeightDecayCoeff = 0.0001;
            networkTrainer.MaxTrainingEpochs = 50;
            networkTrainer.EpochsBeforeRegularization = 0;
            networkTrainer.MiniBatchSize = 32;
            networkTrainer.ConsoleOutputLag = 1; // 1 = print every epoch, N = print every N epochs
            networkTrainer.EvaluateBeforeTraining = true;
            networkTrainer.DropoutFullyConnected = 0.5;
            networkTrainer.Patience = 5;

            // Set output files save paths
            string trainingSavePath = @"C:\Users\jacopo\Dropbox\Chalmers\MSc thesis\Results\LossError\";
            networkTrainer.TrainingEpochSavePath = trainingSavePath + "trainingEpochs.txt";
            networkTrainer.ValidationEpochSavePath = trainingSavePath + "validationEpochs.txt";

            networkTrainer.NetworkOutputFilePath = @"C:\Users\jacopo\Dropbox\Chalmers\MSc thesis\Results\Networks\";
             
            
            networkTrainer.Train(network, trainingSet, validationSet);
            //networkTrainer.Train(network, testSet, validationSet);
            

            /*****************************************************
             * (4) Test network
             ****************************************************/
            Console.WriteLine("\nFINAL EVALUATION:");

            //string summaryFilePath = @"C:\Users\jacopo\Dropbox\Chalmers\MSc thesis\Results\Summaries\";

            // Load best network from file

            NeuralNetwork bestNetwork = Utils.LoadNetworkFromFile(@"C:\Users\jacopo\Dropbox\Chalmers\MSc thesis\Results\Networks\", network.Name);
            network.Set("MiniBatchSize", 32); // this SHOULDN'T matter!
            network.InitializeParameters("load");

            NetworkEvaluator networkEvaluator = new NetworkEvaluator();
            double loss;
            double error;

            networkEvaluator.EvaluateNetwork(network, trainingSet, out loss, out error);
            Console.WriteLine("\nValidation set:\n\tLoss = {0}\n\tError = {1}", loss, error);

            networkEvaluator.EvaluateNetwork(network, validationSet, out loss, out error);
            Console.WriteLine("\nValidation set:\n\tLoss = {0}\n\tError = {1}", loss, error);

            networkEvaluator.EvaluateNetwork(network, testSet, out loss, out error);
            Console.WriteLine("\nTest set:\n\tLoss = {0}\n\tError = {1}", loss, error);
            
#if GRADIENT_CHECK
            GradientChecker.Check(network, reducedMNIST);
#endif
            
            // Save filters of first conv layer
            Utils.SaveFilters(bestNetwork, @"C:\Users\jacopo\Dropbox\Chalmers\MSc thesis\Results\Filters\" + network.Name + "_filters.txt");
        
        }
    }
}
