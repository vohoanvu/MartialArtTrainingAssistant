import { useState, useEffect } from 'react';

type NotificationPopupProps = {
    bannerTitle: string;
    videoTitle: string;
    userName: string;
    isVisible: boolean;
    onClose: () => void;
};

const NotificationPopup = ({ bannerTitle, videoTitle, userName, isVisible, onClose } : NotificationPopupProps) => 
{
    const [show, setShow] = useState(isVisible);
    const [timer, setTimer] = useState(5);

    useEffect(() => {
        setShow(isVisible);

        if (isVisible) {
            // Start the countdown timer
            const countdown = setInterval(() => {
                setTimer((prevTimer) => (prevTimer > 0 ? prevTimer - 1 : 0));
            }, 1000);
      
            // Automatically close the notification after 5 seconds
            const closeTimer = setTimeout(() => {
                setShow(false);
                onClose(); // Callback to notify parent component
            }, 5000);

            return () => {
                clearInterval(countdown);
                clearTimeout(closeTimer);
            };
        }
    }, [isVisible, onClose]);

    if (!show) return null;

    return (
        <div className={`fixed top-0 inset-x-0 z-50 bg-blue-600 text-white p-4 shadow-lg transition-opacity duration-300 ease-out ${
            show ? 'opacity-100' : 'opacity-0'
        }`}>
            <div className="max-w-2xl mx-auto flex justify-between items-center">
                <div className="flex-grow">
                    <strong>{bannerTitle}</strong>
                    <p className="font-bold text-lg">{videoTitle}</p>
                    <p className="text-sm">Shared by: {userName}</p>
                </div>
                <button onClick={() => setShow(false)} className="ml-4 text-white text-lg font-semibold focus:outline-none">
                    &times;
                </button>
            </div>
            <div className="w-full bg-blue-800 absolute bottom-0 left-0 h-1">
                <div className="bg-blue-300 h-full transition-width ease-linear duration-[5000ms]" style={{ width: `${(timer / 5) * 100}%` }}></div>
            </div>
        </div>
    );
};

export default NotificationPopup;