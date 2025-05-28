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
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-50">
            <Card className="w-[400px] shadow-lg">
                <CardHeader>
                    <CardTitle>{title}</CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-muted-foreground">{message}</p>
                </CardContent>
                <CardFooter className="flex justify-end space-x-2">
                    <Button
                        variant="outline"
                        onClick={onCancel}
                    >
                        Cancel
                    </Button>
                    <Button
                        variant="default"
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