import { ReactElement, useEffect, useState } from "react";
import useAuthStore from "@/store/authStore.ts";
import { useNavigate } from "react-router-dom";
import { TrainingSessionResponse } from "@/types/global";
import { getMyTrainingSessions } from "@/services/api";
import { DataTable } from "@/components/ui/data-table";
import { ColumnDef } from '@tanstack/react-table';
import { Button } from "@/components/ui/button";
import { Pencil, Settings } from "lucide-react";
import { PageWrapper } from "@/components/zen/PageWrapper";
import { SectionHeader } from "@/components/zen/SectionHeader";

export default function ClassSession(): ReactElement {
    const isLogged = useAuthStore((state) => state.loginStatus);
    const navigate = useNavigate();
    const [data, setData] = useState<TrainingSessionResponse[] | null>(null);
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const hydrate = useAuthStore((state) => state.hydrate);
    const user = useAuthStore((state) => state.user);

    useEffect(() => {
        switch (isLogged) {
            case "authenticated":
                fetchData();
                break;
            case "unauthenticated":
                navigate("/login");
                break;
            case "pending":
                break;
            default:
                break;
        }
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [isLogged]);

    const fetchData = async () => {
        try {
            const trainingSessions = await getMyTrainingSessions({ jwtToken, refreshToken, hydrate });
            setData(trainingSessions);
        } catch (error) {
            console.error("Error fetching training sessions: ", error);
        }
    };

    const handleCreateNewSession = () => {
        navigate('/create-session');
    };

    const trainingSessionColumns: ColumnDef<TrainingSessionResponse, unknown>[] = [
        {
            header: 'Training Date',
            accessorKey: 'trainingDate' as keyof TrainingSessionResponse,
            cell: ({ row }) => {
                const date = new Date(row.original.trainingDate);
                return <span className="font-mono text-sm text-slate-zen400">{date.toISOString().split('T')[0]}</span>;
            },
        },
        {
            header: 'Start Time',
            accessorKey: 'trainingTime' as keyof TrainingSessionResponse,
            cell: ({ row }) => {
                const date = new Date(row.original.trainingDate);
                return <span className="font-mono text-sm text-slate-zen400">{date.toTimeString().split(' ')[0]}</span>;
            },
        },
        {
            header: 'Description',
            accessorKey: 'description' as keyof TrainingSessionResponse,
            cell: ({ row }) => (
                <span className="font-sans text-sm font-medium text-ink-400">{row.original.description}</span>
            ),
        },
        {
            header: 'Capacity',
            accessorKey: 'capacity' as keyof TrainingSessionResponse,
        },
        {
            header: 'Duration (hours)',
            accessorKey: 'duration' as keyof TrainingSessionResponse,
        },
        {
            header: 'Status',
            accessorKey: 'status' as keyof TrainingSessionResponse,
        },
        {
            header: 'Level',
            accessorKey: 'targetLevel' as keyof TrainingSessionResponse,
        },
        {
            header: 'Actions',
            cell: ({ row }) => <ActionCell sessionId={row.original.id} />
        }
    ];

    const ActionCell = ({ sessionId }: { sessionId: number }) => {
        const navigate = useNavigate();

        const handleEdit = () => {
            navigate(`/edit-session/${sessionId}`);
        };

        const handleCheckIn = () => {
            navigate(`/session-details/${sessionId}`);
        };

        const handleManage = () => {
            navigate(`/session-details/${sessionId}`);
        };

        return (
            <div className="flex items-center gap-2">
                {user && user.fighterInfo?.role == 0 ? (
                    <Button variant="secondary" size="sm" onClick={handleCheckIn}>
                        Check-In
                    </Button>
                ) : (
                    <>
                        <button
                            onClick={handleEdit}
                            className="w-8 h-8 flex items-center justify-center rounded-sm border border-[rgba(60,50,40,0.10)] bg-parchment-50 text-slate-zen400 cursor-pointer hover:bg-parchment-200 hover:text-ink-400 hover:border-[rgba(60,50,40,0.18)] transition-all duration-fast ease-zen"
                        >
                            <Pencil size={14} />
                        </button>
                        <button
                            onClick={handleManage}
                            className="w-8 h-8 flex items-center justify-center rounded-sm border border-[rgba(60,50,40,0.10)] bg-parchment-50 text-slate-zen400 cursor-pointer hover:bg-parchment-200 hover:text-ink-400 hover:border-[rgba(60,50,40,0.18)] transition-all duration-fast ease-zen"
                        >
                            <Settings size={14} />
                        </button>
                    </>
                )}
            </div>
        );
    };

    return (
        <PageWrapper>
            <SectionHeader
                eyebrow="Class Management"
                title="Training Sessions"
                description="Find your training session to check in, or create a new one below."
            />

            {data ? (
                <div className="mt-4">
                    <DataTable
                        columns={trainingSessionColumns}
                        data={data}
                        title="Active Training Sessions"
                    />
                    <div className="flex justify-start mt-5">
                        <Button variant="dark" size="md" onClick={handleCreateNewSession} disabled={user?.fighterInfo?.role == 0}>
                            Create New Session
                        </Button>
                    </div>
                </div>
            ) : (
                <p className="font-sans text-sm text-slate-zen400 mt-6">Loading data from API...</p>
            )}
        </PageWrapper>
    );
}
