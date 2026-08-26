import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { governmentEntitiesApi } from '@/api/endpoints';
import { LoadingIndicator, SuccessMessage } from '@/components/Feedback';
import { GovernmentEntityForm } from '@/components/GovernmentEntityForm';
import { useAsyncData } from '@/hooks/useAsyncData';
import type { UpdateGovernmentEntityRequest } from '@/types/api';

/**
 * Alta de una entidad gubernamental, correspondiente a la opcion "Crear
 * registro" de la maqueta.
 *
 * Al guardar se ofrece la opcion de capturar otra entidad o ir a la consulta,
 * que es el flujo natural cuando se cargan varios registros seguidos.
 */
export function CreateGovernmentEntityPage() {
  const navigate = useNavigate();

  const catalogsQuery = useAsyncData(() => governmentEntitiesApi.getCatalogs(), []);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<unknown>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formKey, setFormKey] = useState(0);

  async function handleSubmit(values: UpdateGovernmentEntityRequest) {
    setIsSubmitting(true);
    setSubmitError(null);
    setSuccessMessage(null);

    try {
      await governmentEntitiesApi.create({
        name: values.name,
        category: values.category,
        stateBranch: values.stateBranch,
        sector: values.sector,
      });

      setSuccessMessage(`La entidad "${values.name}" se registro correctamente.`);

      // Cambiar la clave del formulario lo reinicia con los valores vacios sin
      // necesidad de mantener el estado de cada campo en esta pantalla.
      setFormKey((previousKey) => previousKey + 1);
      await catalogsQuery.reload();
    } catch (error) {
      setSubmitError(error);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2 className="card__title">Nueva entidad gubernamental</h2>
          <p className="card__description">
            El registro se guarda en el archivo de texto plano ubicado en el directorio
            <code> Database </code> del proyecto de la API.
          </p>
        </div>
        <button
          type="button"
          className="button button--secondary"
          onClick={() => navigate('/entidades')}
        >
          Ir a la consulta
        </button>
      </div>

      <SuccessMessage message={successMessage} />

      {catalogsQuery.isLoading ? (
        <LoadingIndicator label="Cargando catalogos..." />
      ) : (
        <GovernmentEntityForm
          key={formKey}
          catalogs={
            catalogsQuery.data ?? { categories: [], sectors: [], stateBranches: [] }
          }
          isSubmitting={isSubmitting}
          submitError={submitError}
          onSubmit={handleSubmit}
        />
      )}
    </section>
  );
}
