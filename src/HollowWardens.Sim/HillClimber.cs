namespace HollowWardens.Sim;

using HollowWardens.Core.Run;

/// <summary>
/// Optimises StrategyParams using a momentum-biased hill-climbing algorithm.
///
/// Algorithm (§5 of adaptive-bot-design.md):
///   - Accept any improvement (greedy hill climb)
///   - 70% chance: pick a param that improved recently (last 20 iters)
///   - 30% chance: pick uniformly at random (exploration)
///   - Shake every shakeInterval iterations: perturb 4 random params by ±1–3
///   - Converge early if best score hasn't improved by ≥0.5 in last 40 iterations
/// </summary>
public static class HillClimber
{
    public record HistoryEntry(int Iteration, double Score, string? ParamChanged);

    /// <summary>
    /// Optimises strategy parameters using the provided evaluator function.
    /// </summary>
    /// <param name="startParams">Starting parameter set (use StrategyDefaults.Root/Ember).</param>
    /// <param name="evaluator">Score function: given params, returns a double score (higher = better).</param>
    /// <param name="maxIterations">Maximum optimisation iterations.</param>
    /// <param name="shakeInterval">Apply a shake every N iterations to escape local maxima.</param>
    /// <param name="convergenceWindow">Stop if best score doesn't improve by convergenceMinImprovement in this many iterations.</param>
    /// <param name="convergenceMinImprovement">Minimum improvement required to continue.</param>
    /// <param name="rng">Optional RNG for reproducible runs.</param>
    /// <param name="onProgress">Optional callback: (iteration, bestScore, paramChanged) for progress reporting.</param>
    public static (StrategyParams BestParams, double BestScore, List<HistoryEntry> History) Optimise(
        StrategyParams startParams,
        Func<StrategyParams, double> evaluator,
        int maxIterations = 200,
        int shakeInterval = 60,
        int convergenceWindow = 40,
        double convergenceMinImprovement = 0.5,
        Random? rng = null,
        Action<int, double, string?>? onProgress = null)
    {
        rng ??= new Random();
        var allParams = StrategyParams.PerturbableParams;

        // Track which iteration each param last improved the score
        var lastImprovedAt = new Dictionary<string, int>(allParams.Count);
        foreach (var p in allParams)
            lastImprovedAt[p] = -100;

        var current   = startParams.Clone();
        double currentScore = evaluator(current);
        var best      = current;
        double bestScore    = currentScore;

        var history = new List<HistoryEntry>(maxIterations + 1)
        {
            new(0, bestScore, null)
        };

        onProgress?.Invoke(0, bestScore, null);

        for (int iter = 1; iter <= maxIterations; iter++)
        {
            // ── Shake every shakeInterval iters to escape local maxima ──────
            if (iter % shakeInterval == 0)
            {
                var shaken = best.WithShake(rng, 4);
                double shakenScore = evaluator(shaken);
                current = shaken;
                currentScore = shakenScore;
                if (shakenScore > bestScore) { best = shaken; bestScore = shakenScore; }

                history.Add(new(iter, bestScore, "SHAKE"));
                onProgress?.Invoke(iter, bestScore, "SHAKE");
                continue;
            }

            // ── Momentum-biased param selection ─────────────────────────────
            string paramName;
            var recentImprovers = allParams
                .Where(p => lastImprovedAt[p] > iter - 20)
                .ToList();

            if (recentImprovers.Count > 0 && rng.NextDouble() < 0.7)
                paramName = recentImprovers[rng.Next(recentImprovers.Count)];
            else
                paramName = allParams[rng.Next(allParams.Count)];

            // ── Evaluate candidate ───────────────────────────────────────────
            var candidate      = current.WithPerturbation(paramName, rng);
            double candidateScore = evaluator(candidate);

            if (candidateScore >= currentScore)
            {
                current      = candidate;
                currentScore = candidateScore;
                lastImprovedAt[paramName] = iter;

                if (candidateScore > bestScore)
                {
                    best      = candidate;
                    bestScore = candidateScore;
                }
            }

            history.Add(new(iter, bestScore, paramName));
            onProgress?.Invoke(iter, bestScore, paramName);

            // ── Convergence check ────────────────────────────────────────────
            if (iter > convergenceWindow * 2)
            {
                double oldBest = history[history.Count - convergenceWindow - 1].Score;
                if (bestScore - oldBest < convergenceMinImprovement)
                    break;
            }
        }

        return (best, bestScore, history);
    }

    /// <summary>
    /// Score function from §5.1 of adaptive-bot-design.md.
    /// score = clean% - 3.0×breach% - 0.5×|weathered%-27.5| + 0.1×avg_heart_damage - 0.2×|avg_weave-16|
    /// Target ~50–60 for a well-balanced encounter.
    /// </summary>
    public static double ComputeScore(
        double cleanPct, double weatheredPct, double breachPct,
        double avgHeartDamage, double avgFinalWeave)
    {
        return cleanPct
             - 3.0 * breachPct
             - 0.5 * Math.Abs(weatheredPct - 27.5)
             + 0.1 * avgHeartDamage
             - 0.2 * Math.Abs(avgFinalWeave - 16.0);
    }
}
