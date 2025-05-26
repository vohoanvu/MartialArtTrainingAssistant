import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
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
import { AttendanceRecordDto } from '@/types/global';
import { takeAttendance } from '@/services/api';
import useAuthStore from '@/store/authStore';
import { useAttendanceStore } from '@/store/attendanceStore';

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

const AttendancePage = () => {
    const { sessionId } = useParams<{ sessionId: string }>();
    const navigate = useNavigate();
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const hydrate = useAuthStore((state) => state.hydrate);

    const {
        sessionRecords,
        setSessionRecords,
        updateRecord,
        clearSessionRecords,
        sessionDetails,
    } = useAttendanceStore();

    // Use session capacity for total rows
    const DEFAULT_TOTAL_ROWS = sessionDetails[sessionId!]?.capacity ?? 10;

    // Initialize data with DEFAULT_TOTAL_ROWS empty records
    useEffect(() => {
        if (!sessionRecords[sessionId!]) {
            const initialRecords = Array(DEFAULT_TOTAL_ROWS)
                .fill(null)
                .map(() => createEmptyRecord());
            setSessionRecords(sessionId!, initialRecords);
        }
    }, [sessionId, setSessionRecords, DEFAULT_TOTAL_ROWS]);

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

    // Function to check if a row is complete
    const isRowComplete = (record: AttendanceRecordDto) => {
        return record.fighterName.trim() !== '' &&
            record.weight > 0 &&
            record.height > 0;
    };

    // Find the indices of editable rows: all complete rows + up to 3 incomplete rows
    const getEditableRowIndices = (records: AttendanceRecordDto[]) => {
        const completeIndices = records
            .map((rec, idx) => (isRowComplete(rec) ? idx : null))
            .filter(idx => idx !== null) as number[];
        const incompleteIndices = records
            .map((rec, idx) => (!isRowComplete(rec) ? idx : null))
            .filter(idx => idx !== null) as number[];
        return [...completeIndices, ...incompleteIndices.slice(0, EDITABLE_EMPTY_ROWS_COUNT)];
    };

    const records = sessionRecords[sessionId!] || [];
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
                        onBlur={() => updateRecord(sessionId!, row.index, 'fighterName', localValue)}
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
                        onBlur={() => updateRecord(sessionId!, row.index, 'birthdate', new Date(localValue))}
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
                            onBlur={() => updateRecord(sessionId!, row.index, 'weight', parseFloat(localValue))}
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
                            onBlur={() => updateRecord(sessionId!, row.index, 'height', parseFloat(localValue))}
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
                        onValueChange={(value) => updateRecord(sessionId!, row.index, 'beltColor', value)}
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
                        onValueChange={(value) => updateRecord(sessionId!, row.index, 'gender', value)}
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
        data: sessionRecords[sessionId!] || [],
        columns,
        getCoreRowModel: getCoreRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
    });

    const handleSubmit = async () => {
        try {
            const records = sessionRecords[sessionId!] || [];
            const validRecords = records.filter(record =>
                record.fighterName.trim() !== '' &&
                record.weight > 0 &&
                record.height > 0
            );

            if (validRecords.length === 0) {
                alert('Please enter at least one valid record');
                return;
            }

            const response = await takeAttendance(
                Number(sessionId),
                { records: validRecords },
                { jwtToken, refreshToken, hydrate }
            );

            if (response.success) {
                clearSessionRecords(sessionId!);
                navigate(`/sessions/${sessionId}`);
            } else {
                alert(response.message || 'Failed to record attendance');
            }
        } catch (error) {
            console.error('Error submitting attendance:', error);
            alert('Failed to record attendance');
        }
    };

    const handleClear = () => {
        clearSessionRecords(sessionId!);
        const initialRecords = Array(DEFAULT_TOTAL_ROWS)
            .fill(null)
            .map(() => createEmptyRecord());
        setSessionRecords(sessionId!, initialRecords);
    };

    return (
        <div className="container mx-auto p-6 max-w-7xl h-screen flex flex-col">
            <div className="flex justify-between items-center mb-6">
                <h1 className="text-3xl font-bold">Take Attendance</h1>
                <div className="space-x-4">
                    <Button
                        variant="outline"
                        onClick={() => navigate(`/session-details/${sessionId}`)}
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
                        onClick={handleSubmit}
                        className="bg-primary hover:bg-primary/90 text-primary-foreground"
                    >
                        Finalize Attendance
                    </Button>
                </div>
            </div>

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

            <div className="mt-4 text-sm text-muted-foreground">
                <p>* Data is automatically saved locally on blur for text fields</p>
                <p>* Weight should be entered in kilograms (e.g., 70.5)</p>
                <p>* Height should be entered in centimeters (e.g., 175.0)</p>
                <p>* Up to {EDITABLE_EMPTY_ROWS_COUNT} empty rows are available for editing at a time</p>
                <p>* Completed rows remain editable for updates</p>
            </div>
        </div>
    );
};

export default AttendancePage;