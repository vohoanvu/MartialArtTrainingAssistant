import React from 'react';

interface GradeGaugeProps {
    grade: number; // 0-100
    editable?: boolean;
    onChange?: (grade: number) => void;
}

const gradeColor = (g: number): string => {
    if (g >= 80) return 'text-green-600 border-green-500';
    if (g >= 60) return 'text-samurai-400 border-samurai-400';
    if (g >= 40) return 'text-amber-600 border-amber-500';
    return 'text-blood-600 border-blood-500';
};

/** Compact 0-100 technical grade indicator. Editable variant renders a number input. */
const GradeGauge: React.FC<GradeGaugeProps> = ({ grade, editable, onChange }) => {
    const clamped = Math.max(0, Math.min(100, Math.round(grade ?? 0)));
    return (
        <div className={`flex items-center justify-center w-20 h-20 rounded-full border-4 ${gradeColor(clamped)}`}>
            {editable ? (
                <input
                    type="number"
                    min={0}
                    max={100}
                    value={clamped}
                    onChange={(e) => onChange?.(Math.max(0, Math.min(100, Number(e.target.value))))}
                    className="w-12 text-2xl font-bold text-center bg-transparent focus:outline-none"
                    aria-label="technical grade"
                />
            ) : (
                <span className="text-2xl font-bold">{clamped}</span>
            )}
        </div>
    );
};

export default GradeGauge;
