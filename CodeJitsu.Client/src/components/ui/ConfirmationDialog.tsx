import React from 'react';
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

interface ConfirmationDialogProps {
    title: string;
    message: string;
    isOpen: boolean;
    onConfirm: () => void;
    onCancel: () => void;
}

const ConfirmationDialog: React.FC<ConfirmationDialogProps> = ({ 
    title, 
    message, 
    isOpen, 
    onConfirm, 
    onCancel 
}) => {
    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 bg-parchment-100/80 backdrop-blur-sm flex items-center justify-center z-50">
            <Card className="w-[400px] shadow-zen-lg bg-parchment-50 border-[rgba(60,50,40,0.10)]">
                <CardHeader>
                    <CardTitle className="font-serif text-ink-400">{title}</CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-slate-zen400 font-sans">{message}</p>
                </CardContent>
                <CardFooter className="flex justify-end space-x-2">
                    <Button
                        type="button"
                        variant="secondary"
                        onClick={onCancel}
                    >
                        Cancel
                    </Button>
                    <Button
                        type="button"
                        variant="primary"
                        onClick={onConfirm}
                    >
                        Confirm
                    </Button>
                </CardFooter>
            </Card>
        </div>
    );
};

export default ConfirmationDialog;