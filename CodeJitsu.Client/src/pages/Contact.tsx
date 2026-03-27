// Contact.tsx
import { useTranslation } from 'react-i18next';

//Unused component
export default function Contact() {
    const { t } = useTranslation();

    return (
        <div className="container mx-auto p-4 max-w-xl text-center">
            <h1 className="text-3xl font-bold mb-4">{t("contact.title")}</h1>
            <p className="mb-4">{t("contact.description")}</p>
            <div className="space-y-4">
                <p>{t("contact.email")} <a href="mailto:vohoanvu96@gmail.com" className="text-blue-500">vohoanvu96@gmail.com</a></p>
                <p>{t("contact.github")} <a href="https://github.com/vohoanvu" className="text-blue-500">github.com/vohoanvu</a></p>
            </div>
        </div>
    );
}
