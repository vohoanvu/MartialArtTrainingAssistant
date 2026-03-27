import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    createColumnHelper,
    flexRender,
    getCoreRowModel,
    useReactTable,
    getPaginationRowModel,
} from '@tanstack/react-table';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from '@/components/ui/select';
import { AttendanceRecordDto, SessionDetailViewModel } from '@/types/global';
import { takeWalkInAttendance } from '@/services/api';
import { useAttendanceStore } from '@/store/attendanceStore';
import ConfirmationDialog from '../ui/ConfirmationDialog';

// Configuration constants
const EDITABLE_EMPTY_ROWS_COUNT = 2; // Number of empty rows that can be edited at once

const columnHelper = createColumnHelper<AttendanceRecordDto>();

const GENDER_OPTIONS = [
    { value: "Male", label: 'Male' },
    { value: "Female", label: 'Female' }
];

const BELT_COLOR_OPTIONS = [
    { value: "None", label: 'Unranked' },
    { value: "White", label: 'White' },
    { value: "Blue", label: 'Blue' },
    { value: "Purple", label: 'Purple' },
    { value: "Brown", label: 'Brown' },
    { value: "Black", label: 'Black' }
];

const FAKE_NAMES = [
    "John Smith", "Emma Johnson", "Michael Brown", "Sarah Davis", "James Wilson",
    "Olivia Taylor", "William Anderson", "Sophia Martinez", "David Thomas", "Isabella Lee"
];

interface AttendancePageProps {
    trainingSessionId: number;
    sessionDetailsViewModel : SessionDetailViewModel | null;
    onCancel: () => void;
}

export const AttendancePage = ({ trainingSessionId, sessionDetailsViewModel, onCancel }: AttendancePageProps) => {
    const navigate = useNavigate();
    const [isFinalizeDialogOpen, setIsFinalizeDialogOpen] = useState(false);
    const [lbsInput, setLbsInput] = useState('');
    const [ftInput, setFtInput] = useState('');
    const [isAttendanceFinalizing, setIsAttendanceFinalizing] = useState(false);

    const {
        sessionRecords,
        setSessionRecords,
        updateRecord,
        clearSessionRecords,
    } = useAttendanceStore();

    // Use session capacity for total rows
    const DEFAULT_TOTAL_ROWS = sessionDetailsViewModel?.capacity ?? 10;

    // Initialize data with DEFAULT_TOTAL_ROWS empty records
    useEffect(() => {
        if (!sessionRecords[trainingSessionId!]) {
            const initialRecords = Array(DEFAULT_TOTAL_ROWS)
                .fill(null)
                .map(() => createEmptyRecord());
            setSessionRecords(trainingSessionId, initialRecords);
        }
    }, [trainingSessionId, setSessionRecords, DEFAULT_TOTAL_ROWS]);

    // Function to create an empty record
    function createEmptyRecord(): AttendanceRecordDto {
        return {
            fighterName: '',
            birthdate: new Date(),
            weight: 0,
            height: 0,
            beltColor: "White",
            gender: "Male"
        };
    }

    // Function to generate a random birthdate (between 10 and 40 years ago)
    const generateRandomBirthdate = () => {
        const start = new Date();
        start.setFullYear(start.getFullYear() - 40);
        const end = new Date();
        end.setFullYear(end.getFullYear() - 10);
        const randomDate = new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
        return randomDate;
    };

    // Function to generate fake attendance records
    const generateFakeRecords = () => {
        const records: AttendanceRecordDto[] = Array(DEFAULT_TOTAL_ROWS)
            .fill(null)
            .map((_, index) => ({
                fighterName: FAKE_NAMES[index % FAKE_NAMES.length] || `Student ${index + 1}`,
                birthdate: generateRandomBirthdate(),
                weight: parseFloat((Math.random() * (100 - 50) + 50).toFixed(1)), // Weight between 50-100 kg
                height: parseFloat((Math.random() * (200 - 150) + 150).toFixed(1)), // Height between 150-200 cm
                beltColor: BELT_COLOR_OPTIONS[Math.floor(Math.random() * BELT_COLOR_OPTIONS.length)].value,
                gender: GENDER_OPTIONS[Math.floor(Math.random() * GENDER_OPTIONS.length)].value
            }));
        setSessionRecords(trainingSessionId!, records);
    };

    // Function to check if a row is complete
    const isRowComplete = (record: AttendanceRecordDto) => {
        return record.fighterName.trim() !== '' &&
            record.weight > 0 &&
            record.height > 0;
    };

    // Find the indices of editable rows: all complete rows + up to 2 incomplete rows
    const getEditableRowIndices = (records: AttendanceRecordDto[]) => {
        const completeIndices = records
            .map((rec, idx) => (isRowComplete(rec) ? idx : null))
            .filter(idx => idx !== null) as number[];
        const incompleteIndices = records
            .map((rec, idx) => (!isRowComplete(rec) ? idx : null))
            .filter(idx => idx !== null) as number[];
        return [...completeIndices, ...incompleteIndices.slice(0, EDITABLE_EMPTY_ROWS_COUNT)];
    };

    const records = sessionRecords[trainingSessionId!] || [];
    const editableRowIndices = getEditableRowIndices(records);

    const columns = [
        columnHelper.accessor('fighterName', {
            header: 'Name',
            cell: ({ row, getValue }) => {
                const isEditable = editableRowIndices.includes(row.index);
                const [localValue, setLocalValue] = useState(getValue());
                return (
                    <Input
                        value={localValue}
                        onChange={e => setLocalValue(e.target.value)}
                        onBlur={() => updateRecord(trainingSessionId, row.index, 'fighterName', localValue)}
                        placeholder="Enter name"
                        className="w-full"
                        disabled={!isEditable}
                    />
                );
            },
        }),
        columnHelper.accessor('birthdate', {
            header: 'Birthdate',
            cell: ({ row, getValue }) => {
                const isEditable = editableRowIndices.includes(row.index);
                const date = getValue();
                const dateValue = date instanceof Date ? date : new Date(date);
                const [localValue, setLocalValue] = useState(dateValue.toISOString().split('T')[0]);
                return (
                    <Input
                        type="date"
                        value={localValue}
                        onChange={(e) => setLocalValue(e.target.value)}
                        onBlur={() => updateRecord(trainingSessionId, row.index, 'birthdate', new Date(localValue))}
                        className="w-full"
                        disabled={!isEditable}
                    />
                );
            },
        }),
        columnHelper.accessor('weight', {
            header: 'Weight (kg)',
            cell: ({ row, getValue }) => {
                const isEditable = editableRowIndices.includes(row.index);
                const [localValue, setLocalValue] = useState(getValue().toString());
                return (
                    <div className="relative">
                        <Input
                            type="number"
                            value={localValue}
                            onChange={(e) => setLocalValue(e.target.value)}
                            onBlur={() => updateRecord(trainingSessionId, row.index, 'weight', parseFloat(localValue))}
                            placeholder="Enter weight"
                            className="w-full"
                            disabled={!isEditable}
                        />
                        <div className="absolute -bottom-6 left-0 text-xs text-muted-foreground">
                            Format: 70.5
                        </div>
                    </div>
                );
            },
        }),
        columnHelper.accessor('height', {
            header: 'Height (cm)',
            cell: ({ row, getValue }) => {
                const isEditable = editableRowIndices.includes(row.index);
                const [localValue, setLocalValue] = useState(getValue().toString());
                return (
                    <div className="relative">
                        <Input
                            type="number"
                            value={localValue}
                            onChange={(e) => setLocalValue(e.target.value)}
                            onBlur={() => updateRecord(trainingSessionId, row.index, 'height', parseFloat(localValue))}
                            placeholder="Enter height"
                            className="w-full"
                            disabled={!isEditable}
                        />
                        <div className="absolute -bottom-6 left-0 text-xs text-muted-foreground">
                            Format: 175.0
                        </div>
                    </div>
                );
            },
        }),
        columnHelper.accessor('beltColor', {
            header: 'Belt Rank',
            cell: ({ row, getValue }) => {
                const isEditable = editableRowIndices.includes(row.index);
                return (
                    <Select
                        value={getValue()}
                        onValueChange={(value) => updateRecord(trainingSessionId, row.index, 'beltColor', value)}
                        disabled={!isEditable}
                    >
                        <SelectTrigger>
                            <SelectValue placeholder="Select belt rank" />
                        </SelectTrigger>
                        <SelectContent>
                            {BELT_COLOR_OPTIONS.map((option) => (
                                <SelectItem key={option.value} value={option.value}>
                                    {option.label}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                );
            },
        }),
        columnHelper.accessor('gender', {
            header: 'Gender',
            cell: ({ row, getValue }) => {
                const isEditable = editableRowIndices.includes(row.index);
                return (
                    <Select
                        value={getValue()}
                        onValueChange={(value) => updateRecord(trainingSessionId, row.index, 'gender', value)}
                        disabled={!isEditable}
                    >
                        <SelectTrigger>
                            <SelectValue placeholder="Select gender" />
                        </SelectTrigger>
                        <SelectContent>
                            {GENDER_OPTIONS.map((option) => (
                                <SelectItem key={option.value} value={option.value}>
                                    {option.label}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                );
            },
        }),
    ];

    const table = useReactTable({
        data: sessionRecords[trainingSessionId!] || [],
        columns,
        getCoreRowModel: getCoreRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
    });

    const handleSubmit = async () => {
        try {
            setIsAttendanceFinalizing(true);
            const records = sessionRecords[trainingSessionId!] || [];
            const validRecords = records.filter(record =>
                record.fighterName.trim() !== '' &&
                record.weight > 0 &&
                record.height > 0
            );

            if (validRecords.length === 0) {
                alert('Please enter at least one valid record');
                return;
            }

            const response = await takeWalkInAttendance(
                trainingSessionId,
                { records: validRecords },
            );

            if (response.success) {
                clearSessionRecords(trainingSessionId!);
                onCancel();
                navigate(`/session-details/${trainingSessionId}`);
            } else {
                alert(response.message || 'Failed to record attendance');
            }
        } catch (error) {
            console.error('Error submitting attendance:', error);
            alert('Failed to record attendance');
        } finally {
            setIsAttendanceFinalizing(false);
        }
    };

    const handleClear = () => {
        clearSessionRecords(trainingSessionId!);
        const initialRecords = Array(DEFAULT_TOTAL_ROWS)
            .fill(null)
            .map(() => createEmptyRecord());
        setSessionRecords(trainingSessionId!, initialRecords);
    };

    return (
        <div className="container mx-auto p-6 max-w-7xl h-screen flex flex-col">
            <div className="flex justify-between items-center mb-6">
                <h1 className="text-3xl font-bold">Walk-In Attendance</h1>
                <div className="space-x-4">
                    <Button
                        variant="outline"
                        onClick={onCancel}
                    >
                        Go Back
                    </Button>
                    <Button
                        variant="destructive"
                        onClick={handleClear}
                    >
                        Clear
                    </Button>
                    <Button
                        variant="secondary"
                        onClick={generateFakeRecords}
                    >
                        Create Fake Student Data for Testing
                    </Button>
                    <Button
                        onClick={() => setIsFinalizeDialogOpen(true)}
                        className="bg-primary hover:bg-primary/90 text-primary-foreground"
                    >
                        {isAttendanceFinalizing ? "Finalizing..." : "Finalize Attendance"}
                    </Button>
                </div>
            </div>
            <ConfirmationDialog
                title="Finalize Attendance"
                message="Are you sure you want to finalize today class' attendance? This cannot be undone."
                isOpen={isFinalizeDialogOpen}
                onConfirm={() => {
                    handleSubmit();
                    setIsFinalizeDialogOpen(false);
                }}
                onCancel={() => setIsFinalizeDialogOpen(false)}
            />

            <div className="bg-card rounded-lg shadow-sm border">
                <div className="overflow-x-auto">
                    <table className="w-full h-full">
                        <thead className="sticky top-0 bg-card z-10">
                            {table.getHeaderGroups().map(headerGroup => (
                                <tr key={headerGroup.id} className="border-b">
                                    {headerGroup.headers.map(header => (
                                        <th
                                            key={header.id}
                                            className="px-4 py-3 text-left text-sm font-medium text-muted-foreground bg-card"
                                        >
                                            {flexRender(
                                                header.column.columnDef.header,
                                                header.getContext()
                                            )}
                                        </th>
                                    ))}
                                </tr>
                            ))}
                        </thead>
                        <tbody>
                            {table.getRowModel().rows.map(row => (
                                <tr
                                    key={row.id}
                                    className={`border-b hover:bg-muted/50 text-sm ${editableRowIndices.includes(row.index)
                                            ? 'bg-background'
                                            : 'bg-muted/30'
                                        }`}
                                    style={{ height: '36px' }}
                                >
                                    {row.getVisibleCells().map(cell => (
                                        <td key={cell.id} className="px-4 py-1 align-middle">
                                            {flexRender(
                                                cell.column.columnDef.cell,
                                                cell.getContext()
                                            )}
                                        </td>
                                    ))}
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>

            <div className="flex justify-between mt-4">
                <div className="text-sm text-muted-foreground">
                    <p>* Data is automatically saved locally on blur for text fields</p>
                    <p>* Weight should be entered in kilograms (e.g., 70.5)</p>
                    <p>* Height should be entered in centimeters (e.g., 175.0)</p>
                    <p>* Up to {EDITABLE_EMPTY_ROWS_COUNT} empty rows are available for editing at a time</p>
                    <p>* Completed rows remain editable for updates</p>
                </div>
                <div className="text-sm text-muted-foreground text-right">
                    <h3 className="font-semibold mb-1">Metric Conversion Tool</h3>
                    <div className="flex flex-col space-y-2">
                        <div className="flex items-center justify-end">
                            <label className="mr-2">LBS:</label>
                            <Input
                                type="number"
                                value={lbsInput}
                                onChange={(e) => setLbsInput(e.target.value)}
                                placeholder="lbs"
                                className="w-20"
                            />
                            <span className="ml-2">
                                ⇨ {lbsInput ? (parseFloat(lbsInput) * 0.45359237).toFixed(1) : "0"} KG
                            </span>
                        </div>
                        <div className="flex items-center justify-end">
                            <label className="mr-2">FT:</label>
                            <Input
                                type="number"
                                value={ftInput}
                                onChange={(e) => setFtInput(e.target.value)}
                                placeholder="ft"
                                className="w-20"
                            />
                            <span className="ml-2">
                                ⇨ {ftInput ? (parseFloat(ftInput) * 30.48).toFixed(0) : "0"} CM
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default AttendancePage;