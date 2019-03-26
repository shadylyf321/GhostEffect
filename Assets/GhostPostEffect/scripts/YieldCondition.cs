using UnityEngine;
using System;

public class YieldCondition : CustomYieldInstruction
{
     Func<bool> conditionJudge;
    public override bool keepWaiting
    {
        get
        {
            if (!conditionJudge())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public YieldCondition(Func<bool> judge)
    {
        conditionJudge = judge;
    }
}
