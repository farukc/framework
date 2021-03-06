﻿// Accord Statistics Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2016
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Statistics.Models.Markov
{
    using Accord.MachineLearning;
    using Accord.Math;
    using Accord.Statistics.Distributions;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    ///   Base class for (HMM) Sequence Classifiers. 
    ///   This class cannot be instantiated.
    /// </summary>
    /// 
    [Serializable]
    public abstract class BaseHiddenMarkovClassifier<TModel, TDistribution, TObservation> :
        MulticlassLikelihoodClassifierBase<TObservation[]>, IEnumerable<TModel>
        where TModel : HiddenMarkovModel<TDistribution, TObservation>
        where TDistribution : IDistribution<TObservation>
    {

        private TModel[] models;
        private double[] classPriors;

        // Threshold (rejection) model
        private TModel threshold;
        private double weight = 1;


        /// <summary>
        ///   Initializes a new instance of the <see cref="BaseHiddenMarkovClassifier&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="classes">The number of classes in the classification problem.</param>
        /// 
        protected BaseHiddenMarkovClassifier(int classes)
        {
            this.NumberOfOutputs = classes;
            models = new TModel[classes];

            classPriors = new double[classes];
            for (int i = 0; i < classPriors.Length; i++)
                classPriors[i] = 1.0 / classPriors.Length;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="BaseHiddenMarkovClassifier&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="models">The models specializing in each of the classes of the classification problem.</param>
        /// 
        protected BaseHiddenMarkovClassifier(TModel[] models)
        {
            this.models = models;

            classPriors = new double[models.Length];
            for (int i = 0; i < classPriors.Length; i++)
                classPriors[i] = 1.0 / classPriors.Length;
        }

        /// <summary>
        ///   Gets or sets the threshold model.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   For gesture spotting, Lee and Kim introduced a threshold model which is
        ///   composed of parts of the models in a hidden Markov sequence classifier.</para>
        /// <para>
        ///   The threshold model acts as a baseline for decision rejection. If none of
        ///   the classifiers is able to produce a higher likelihood than the threshold
        ///   model, the decision is rejected.</para>
        /// <para>
        ///   In the original Lee and Kim publication, the threshold model is constructed
        ///   by creating a fully connected ergodic model by removing all outgoing transitions
        ///   of states in all gesture models and fully connecting those states.</para>
        /// <para>
        ///   References:
        ///   <list type="bullet">
        ///     <item><description>
        ///        H. Lee, J. Kim, An HMM-based threshold model approach for gesture
        ///        recognition, IEEE Trans. Pattern Anal. Mach. Intell. 21 (10) (1999)
        ///        961–973.</description></item>
        ///   </list></para>
        /// </remarks>
        /// 
        public TModel Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }

        /// <summary>
        ///   Gets or sets a value governing the rejection given by
        ///   a threshold model (if present). Increasing this value
        ///   will result in higher rejection rates. Default is 1.
        /// </summary>
        /// 
        public double Sensitivity
        {
            get { return weight; }
            set { weight = value; }
        }


        /// <summary>
        ///   Gets the collection of models specialized in each 
        ///   class of the sequence classification problem.
        /// </summary>
        /// 
        public TModel[] Models
        {
            get { return models; }
        }

        /// <summary>
        ///   Gets the <see cref="IHiddenMarkovModel">Hidden Markov
        ///   Model</see> implementation responsible for recognizing
        ///   each of the classes given the desired class label.
        /// </summary>
        /// <param name="label">The class label of the model to get.</param>
        /// 
        public TModel this[int label]
        {
            get { return models[label]; }
        }

        /// <summary>
        ///   Gets the number of classes which can be recognized by this classifier.
        /// </summary>
        /// 
        public int Classes
        {
            get { return models.Length; }
        }

        /// <summary>
        ///   Gets the prior distribution assumed for the classes.
        /// </summary>
        /// 
        public double[] Priors
        {
            get { return classPriors; }
        }

        /// <summary>
        /// Computes the log-likelihood that the given input vector
        /// belongs to its decided class.
        /// </summary>
        /// 
        public override double LogLikelihood(TObservation[] input)
        {
            return Math.Log(Probability(input));
        }

        /// <summary>
        /// Computes the likelihood that the given input vector
        /// belongs to its decided class.
        /// </summary>
        /// 
        public override double Probability(TObservation[] input)
        {
            int decision;
            double[] result = Probabilities(input, out decision);
            if (decision == -1)
                return 1.0 - result.Sum();
            return result[decision];
        }

        /// <summary>
        /// Computes the log-likelihood that the given input vector
        /// belongs to its decided class.
        /// </summary>
        /// 
        public override double LogLikelihood(TObservation[] input, out int decision)
        {
            return Math.Log(Probability(input, out decision));
        }

        /// <summary>
        /// Computes the probability that the given input vector
        /// belongs to its decided class.
        /// </summary>
        /// 
        public override double Probability(TObservation[] input, out int decision)
        {
            double[] result = Probabilities(input, out decision);
            if (decision == -1)
                return 1.0 - result.Sum();
            return result[decision];
        }

        /// <summary>
        /// Computes the log-likelihood that the given input vector
        /// belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <returns></returns>
        public override double LogLikelihood(TObservation[] input, int classIndex)
        {
            int decision;
            return Math.Log(Probabilities(input, out decision)[classIndex]);
        }

        /// <summary>
        /// Computes the probabilities that the given input
        /// vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The decided class for the input.</param>
        /// <param name="result">An array where the probabilities will be stored,
        /// avoiding unnecessary memory allocations.</param>
        /// <returns></returns>
        public override double[] Probabilities(TObservation[] input, out int decision, double[] result)
        {
            LogLikelihoods(input, out decision, result);
            return result.Exp(result: result);
        }

        /// <summary>
        /// Predicts a class label vector for the given input vector, returning the
        /// log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The decided class for the input.</param>
        /// <param name="result">An array where the probabilities will be stored,
        /// avoiding unnecessary memory allocations.</param>
        /// <returns></returns>
        public override double[] LogLikelihoods(TObservation[] input, out int decision, double[] result)
        {
            // Evaluate the probability of the sequence for every model in the set
            for (int i = 0; i < models.Length; i++)
                result[i] = models[i].LogLikelihood(input) + Math.Log(classPriors[i]);

            // Get the index of the most likely model
            double maxValue = result.Max(out decision);

            // Compute posterior likelihoods
            double lnsum = Double.NegativeInfinity;
            for (int i = 0; i < result.Length; i++)
                lnsum = Special.LogSum(lnsum, result[i]);

            // Compute threshold model posterior likelihood
            if (threshold != null)
            {
                // Evaluate the current rejection threshold 
                double rejection = threshold.LogLikelihood(input) + Math.Log(weight);

                if (rejection > maxValue)
                    decision = -1; // input should be rejected (does not belong to any of the classes)

                lnsum = Special.LogSum(lnsum, rejection);
            }

            // Normalize if different from zero
            if (lnsum != Double.NegativeInfinity)
                result.Subtract(lnsum, result: result);

            return result;
        }

        /// <summary>
        /// Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">The input vector that should be classified into
        /// one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.</param>
        /// <returns>
        /// A class-label that best described <paramref name="input" /> according
        /// to this classifier.
        /// </returns>
        public override int Decide(TObservation[] input)
        {
            int decision;
            LogLikelihoods(input, out decision);
            return decision;
        }



        /// <summary>
        ///   Returns an enumerator that iterates through the models in the classifier.
        /// </summary>
        /// 
        /// <returns>
        ///   A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that 
        ///   can be used to iterate through the collection.
        /// </returns>
        /// 
        public IEnumerator<TModel> GetEnumerator()
        {
            foreach (var model in models)
                yield return model;
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the models in the classifier.
        /// </summary>
        /// 
        /// <returns>
        ///   A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that 
        ///   can be used to iterate through the collection.
        /// </returns>
        /// 
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (var model in models)
                yield return model;
        }

    }

}
