﻿import {ReactElement, useEffect, useState } from "react";
import useAuthStore from "@/store/authStore.ts";
import {useNavigate} from "react-router-dom";
import { TrainingSessionResponse } from "@/types/global";
import { getMyTrainingSessions } from "@/services/api";
import { DataTable } from "@/components/ui/data-table";

import { ColumnDef } from '@tanstack/react-table';  // or wherever your ColumnDef type is coming from
import { Button } from "@/components/ui/button";

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
        navigate('/create-session'); // Navigate to the TrainingSessionForm component
    };
    
    
    const trainingSessionColumns: ColumnDef<TrainingSessionResponse, unknown>[] = [
        {
            header: 'Training Date',
            accessorKey: 'trainingDate' as keyof TrainingSessionResponse,
            cell: ({ row }) => {
                const date = new Date(row.original.trainingDate);
                const formattedDate = date.toISOString().split('T')[0]; // Format as yyyy-MM-dd
                return formattedDate;
            },
        },
        {
            header: 'Start Time',
            accessorKey: 'trainingTime' as keyof TrainingSessionResponse,
            cell: ({ row }) => {
                const date = new Date(row.original.trainingDate);
                const formattedTime = date.toTimeString().split(' ')[0]; // Format as HH:mm:ss
                return formattedTime;
            },
        },
        {
            header: 'Description',
            accessorKey: 'description' as keyof TrainingSessionResponse,
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
            <div className="flex space-x-2">
                {
                    //Student role is 0, Instructor is 1
                    user && user.fighterInfo?.role == 0 ? (
                        <Button type="button" onClick={handleCheckIn}>
                            Check-In
                        </Button>
                    ) : (
                        <>
                            <Button type="button" onClick={handleEdit}>
                                Edit
                            </Button>
                            <Button type="button" onClick={handleManage}>
                                Manage
                            </Button>
                        </>
                    )
                }
            </div>
        );
    };

    return (
        <div className="flex flex-col items-center space-y-8">
            {/* Training Sessions Section */}
            <div className="w-full max-w-6xl">
                <h1 className="text-2xl font-bold my-4 text-center">
                    Find your training session to check in
                </h1>
                <div className="w-full">
                    <p className="text-center text-lg mb-4">
                        Create new class session below
                    </p>
                </div>
                {data ? (
                    <div className="rounded-lg dark:shadow-accent p-4 transition duration-500 ease-in-out transform hover:scale-105">
                        <DataTable
                            columns={trainingSessionColumns}
                            data={data}
                            title="Active Training Sessions"
                            titleClassName={"text-center text-xl font-bold py-2"}
                        />
                        <div className="flex justify-start mt-4">
                            <Button type="button" onClick={handleCreateNewSession} disabled={user?.fighterInfo?.role == 0}>
                                Create New Session
                            </Button>
                        </div>
                    </div>
                ) : (
                    <p>Loading data from API...</p>
                )}
            </div>
        </div>
    );
}


// export default function Dashboard(): ReactElement {
//     const [data, setData] = useState<WeatherForecast[] | null>(null);

// const isLogged = useAuthStore((state) => state.loginStatus);
// const navigate = useNavigate();
// const jwtToken = useAuthStore((state) => state.accessToken);
// const refreshToken = useAuthStore((state) => state.refreshToken);
// const hydrate = useAuthStore((state) => state.hydrate);

//     const {t} = useTranslation();

//     useEffect(() => {
//         switch (isLogged) {
//             case "authenticated":
//                 fetchData();
//                 break;
//             case "unauthenticated":
//                 navigate("/login");
//                 break;
//             case "pending":
//                 break;
//             default:
//                 break;
//         }
//     }, [isLogged]);

//     const fetchData = async () => {
//         try {
//             const weatherForecast = await getWeatherForecast({
//                 jwtToken,
//                 refreshToken,
//                 hydrate
//             });
//             setData([...weatherForecast]);
//         } catch (error) {
//             console.error("Error fetching weather forecast: ", error);
//         }
//     };

//     return (
//         <div className="flex flex-col items-center">
//             <h1 className="text-2xl font-bold my-4">
//                 {t("dashboard.title")}
//             </h1>
//             <div className="w-1/2">
//                 <p className="text-center text-lg mb-4">
//                     {t("dashboard.subtitle")}
//                 </p>
//             </div>
//             {data ? (
//                 <div className="w-1/2 rounded-lg dark:shadow-accent p-4 transition duration-500 ease-in-out transform hover:scale-105">
//                     <DataTable columns={weatherForecastColumns} data={data} 
//                                 title={t("dashboard.table.title")} 
//                                 titleClassName={"text-center text-xl font-bold py-2"}
//                     />
//                 </div>
                
//             ) : (
//                 <p>Loading data from API...</p>
//             )}
//         </div>   
//     );
// }


