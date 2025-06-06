import { Button } from './ui/button';
import React from 'react';

interface TimeSegmentSelectionProps {
    videoId: number;
    onSave: () => void;
    onCancel: () => void;
    fromTimestamp: string;
    setFromTimestamp: (timestamp: string) => void;
    toTimestamp: string;
    setToTimestamp: (timestamp: string) => void;
}

const TimeSegmentSelection: React.FC<TimeSegmentSelectionProps> = ({
    onSave,
    onCancel,
    fromTimestamp,
    setFromTimestamp,
    toTimestamp,
    setToTimestamp,
}) => {

    const formatTimestamp = (seconds: number): string => {
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = Math.floor(seconds % 60);
        return `${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
    };

    return (
        <div className="p-4 border rounded-md shadow-md">
            <h3 className="text-lg font-semibold mb-4">Selected Video Segment</h3>
            <div className="flex gap-4 mb-4">
                <div className="flex-1">
                    <label className="block text-sm font-medium mb-1">From</label>
                    <input
                        type="text"
                        value={fromTimestamp ? formatTimestamp(parseFloat(fromTimestamp)) : ''}
                        onChange={(e) => {
                            const [minutes, seconds] = e.target.value.split(':').map(Number);
                            if (!isNaN(minutes) && !isNaN(seconds)) {
                                setFromTimestamp((minutes * 60 + seconds).toString());
                            }
                        }}
                        className="w-full p-2 border rounded-md"
                        placeholder="MM:SS"
                    />
                </div>
                <div className="flex-1">
                    <label className="block text-sm font-medium mb-1">To</label>
                    <input
                        type="text"
                        value={toTimestamp ? formatTimestamp(parseFloat(toTimestamp)) : ''}
                        onChange={(e) => {
                            const [minutes, seconds] = e.target.value.split(':').map(Number);
                            if (!isNaN(minutes) && !isNaN(seconds)) {
                                setToTimestamp((minutes * 60 + seconds).toString());
                            }
                        }}
                        className="w-full p-2 border rounded-md"
                        placeholder="MM:SS"
                    />
                </div>
            </div>


            <div className="flex justify-end gap-2">
                <Button variant="default" onClick={onSave}>Save</Button>
                <Button variant="secondary" onClick={onCancel}>Cancel</Button>
            </div>
        </div>
    );
};

export default TimeSegmentSelection;